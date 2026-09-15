#!/usr/bin/env python3
"""Analyze the WAV and manifest emitted by PetEmulator.AySoundDemo."""

from __future__ import annotations

import sys
import wave
import json
import math
from array import array
from pathlib import Path

try:
    import numpy as np
    from scipy.fft import rfft, rfftfreq
except ModuleNotFoundError:  # pragma: no cover - exercised only on minimal hosts
    np = None


def check(condition: bool, message: str) -> bool:
    print(f"PASS: {message}" if condition else f"FAIL: {message}")
    return condition


def rms(signal: np.ndarray) -> float:
    return float(np.sqrt(np.mean(np.square(signal), dtype=np.float64)))


def fallback_rms(signal: list[float]) -> float:
    return math.sqrt(sum(value * value for value in signal) / len(signal))


def fallback_goertzel(signal: list[float], frequency: float, sample_rate: int) -> float:
    omega = 2 * math.pi * frequency / sample_rate
    coefficient = 2 * math.cos(omega)
    previous = 0.0
    before_previous = 0.0
    for value in signal:
        current = value + coefficient * previous - before_previous
        before_previous, previous = previous, current
    return math.sqrt(previous * previous + before_previous * before_previous - coefficient * previous * before_previous)


def fallback_main(wav_path: Path, manifest_path: Path) -> int:
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    with wave.open(str(wav_path), "rb") as source:
        raw = array("h", source.readframes(source.getnframes()))
        sample_rate = source.getframerate()
        channels = source.getnchannels()
    samples = [sum(raw[index + channel] for channel in range(channels)) / (channels * 32767.0) for index in range(0, len(raw), channels)]
    results: list[bool] = []
    results.append(check(sample_rate == manifest["sample_rate"] == 44100 and channels == 2, "WAV is 44.1 kHz stereo"))
    sections = {item["Name"]: item for item in manifest["sections"]}
    for name, item in sections.items():
        if not name.startswith("tone-") or name == "tone-chord-a4-csharp5-e5":
            continue
        signal = samples[item["StartFrame"] : item["StartFrame"] + item["Frames"]]
        middle = signal[len(signal) // 10 : -len(signal) // 10]
        threshold = sum(middle) / len(middle)
        crossings = sum(a <= threshold < b for a, b in zip(middle, middle[1:]))
        peak = crossings * sample_rate / len(middle)
        expected = float(item["ExpectedHz"])
        results.append(check(abs(peak - expected) <= 3.0, f"{name}: spectral zero-crossing peak {peak:.2f} Hz, expected {expected:.2f} Hz"))
    chord = sections["tone-chord-a4-csharp5-e5"]
    chord_signal = samples[chord["StartFrame"] : chord["StartFrame"] + chord["Frames"]]
    chord_signal = [value - sum(chord_signal) / len(chord_signal) for value in chord_signal]
    chord_magnitudes = [fallback_goertzel(chord_signal, target, sample_rate) for target in (440, 554.37, 659.25)]
    results.append(check(min(chord_magnitudes) > max(chord_magnitudes) * 0.05, "tone chord contains A4, C#5, and E5 spectral energy"))
    for name, item in sections.items():
        if not name.startswith("noise-"):
            continue
        signal = samples[item["StartFrame"] : item["StartFrame"] + item["Frames"]]
        centered = [value - sum(signal) / len(signal) for value in signal]
        transitions = sum((a < 0) != (b < 0) for a, b in zip(centered, centered[1:]))
        results.append(check(fallback_rms(centered) > 0.01 and transitions > 20, f"{name}: non-silent broadband noise (sign transitions {transitions})"))
    envelope_expectations = {
        "envelope-0x00": lambda values: values[0] > values[-1] * 2 and values[-1] < 0.04,
        "envelope-0x04": lambda values: max(values) > values[0] * 2 and values[-1] < max(values) * 0.25,
        "envelope-0x08": lambda values: max(values) > min(values) * 2,
        "envelope-0x0A": lambda values: max(values) > min(values) * 2 and values[0] > values[-1] * 0.5,
        "envelope-0x0C": lambda values: max(values) > min(values) * 2,
        "envelope-0x0E": lambda values: max(values) > min(values) * 2 and values[0] < values[-1] * 1.5,
    }
    for name, expectation in envelope_expectations.items():
        item = sections[name]
        signal = samples[item["StartFrame"] : item["StartFrame"] + item["Frames"]]
        window = int(sample_rate * 0.02)
        values = [fallback_rms(signal[offset : offset + window]) for offset in range(window, len(signal) - window, window)]
        results.append(check(expectation(values), f"{name}: 20 ms RMS envelope matches its documented shape"))
    silent = sections["mixer-all-disabled-silent"]
    silent_signal = samples[silent["StartFrame"] : silent["StartFrame"] + silent["Frames"]]
    results.append(check(fallback_rms(silent_signal) < 1e-6, "mixer-all-disabled-silent: near-zero RMS"))
    print(f"SUMMARY: {sum(results)}/{len(results)} checks passed (standard-library fallback; install numpy scipy for FFT path)")
    return 0 if all(results) else 1


def main() -> int:
    wav_path = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("build/ay38910-capability-demo.wav")
    manifest_path = Path(sys.argv[2]) if len(sys.argv) > 2 else wav_path.with_suffix(".json")
    if np is None:
        return fallback_main(wav_path, manifest_path)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    with wave.open(str(wav_path), "rb") as source:
        frames = source.readframes(source.getnframes())
        sample_rate = source.getframerate()
        channels = source.getnchannels()
        samples = np.frombuffer(frames, dtype="<i2").reshape(-1, channels).mean(axis=1) / 32767.0

    results: list[bool] = []
    results.append(check(sample_rate == manifest["sample_rate"] == 44100 and channels == 2, "WAV is 44.1 kHz stereo"))

    sections = {item["Name"]: item for item in manifest["sections"]}
    tone_names = [name for name in sections if name.startswith("tone-") and name != "tone-chord-a4-csharp5-e5"]
    for name in tone_names:
        item = sections[name]
        signal = samples[item["StartFrame"] : item["StartFrame"] + item["Frames"]]
        signal = signal - np.mean(signal)
        window = signal[len(signal) // 10 : -len(signal) // 10]
        spectrum = np.abs(rfft(window * np.hanning(len(window))))
        frequencies = rfftfreq(len(window), 1 / sample_rate)
        peak = float(frequencies[np.argmax(spectrum[1:]) + 1])
        expected = float(item["ExpectedHz"])
        results.append(check(abs(peak - expected) <= 3.0, f"{name}: FFT peak {peak:.2f} Hz, expected {expected:.2f} Hz"))

    chord = sections["tone-chord-a4-csharp5-e5"]
    chord_signal = samples[chord["StartFrame"] : chord["StartFrame"] + chord["Frames"]]
    chord_spectrum = np.abs(rfft((chord_signal - np.mean(chord_signal)) * np.hanning(len(chord_signal))))
    chord_freqs = rfftfreq(len(chord_signal), 1 / sample_rate)
    chord_peaks = [float(chord_freqs[index]) for index in np.argsort(chord_spectrum[1:])[-12:] + 1]
    results.append(check(all(any(abs(peak - target) <= 4 for peak in chord_peaks) for target in (440, 554.37, 659.25)), "tone chord contains A4, C#5, and E5 peaks"))

    for name, item in sections.items():
        if not name.startswith("noise-"):
            continue
        signal = samples[item["StartFrame"] : item["StartFrame"] + item["Frames"]]
        centered = signal - np.mean(signal)
        spectrum = np.abs(rfft(centered * np.hanning(len(centered))))[1:]
        power = spectrum * spectrum
        flatness = float(np.exp(np.mean(np.log(power + 1e-20))) / (np.mean(power) + 1e-20))
        peak_ratio = float(np.max(power) / (np.mean(power) + 1e-20))
        results.append(check(rms(centered) > 0.01 and flatness > 0.01 and peak_ratio < 200, f"{name}: non-silent broadband noise (flatness {flatness:.3f}, peak/avg {peak_ratio:.1f})"))

    envelope_expectations = {
        "envelope-0x00": lambda values: values[0] > values[-1] * 2 and values[-1] < 0.04,
        "envelope-0x04": lambda values: max(values) > values[0] * 2 and values[-1] < max(values) * 0.25,
        "envelope-0x08": lambda values: values.max() > values.min() * 2,
        "envelope-0x0A": lambda values: values.max() > values.min() * 2 and values[0] > values[-1] * 0.5,
        "envelope-0x0C": lambda values: values.max() > values.min() * 2,
        "envelope-0x0E": lambda values: values.max() > values.min() * 2 and values[0] < values[-1] * 1.5,
    }
    for name, expectation in envelope_expectations.items():
        item = sections[name]
        signal = samples[item["StartFrame"] : item["StartFrame"] + item["Frames"]]
        window = max(1, int(sample_rate * 0.02))
        values = np.array([rms(signal[offset : offset + window]) for offset in range(0, len(signal) - window + 1, window)])
        # Ignore the first and last windows, which can straddle section boundaries.
        values = values[1:-1]
        results.append(check(bool(expectation(values)), f"{name}: 20 ms RMS envelope matches its documented shape"))

    silent = sections["mixer-all-disabled-silent"]
    silent_signal = samples[silent["StartFrame"] : silent["StartFrame"] + silent["Frames"]]
    results.append(check(rms(silent_signal) < 1e-6, "mixer-all-disabled-silent: near-zero RMS"))
    print(f"SUMMARY: {sum(results)}/{len(results)} checks passed")
    return 0 if all(results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
