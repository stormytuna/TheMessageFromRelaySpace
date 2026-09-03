using System;
using UnityEngine;
using TRFDS.DataStructures;
using UnityEngine.Events;
using TRFDS.UI;

namespace TRFDS.MonoBehaviours;

public class SongPlayer : MonoBehaviour
{ 
	public static UnityEvent OnSongFinished = new();

	const double FadeTime = 0.01;

	public static SongPlayer Instance;
	public static bool IsPlaying;

	private AudioSource audioSource;

	private MusicNote[] currentSong;
	private int outputSampleRate;
	private double songStartTime;
	private double songEndTime;
	private double[] phases;

	private void Awake() {
		Instance = this;
		audioSource = gameObject.AddComponent<AudioSource>();
	}

	public bool TryPlaySong(SignalMessage message) {
		if (Song.TryParse(message.signals, out Song song)) {
			PlaySong(song.Notes.ToArray());
			return true;
		}

		return false;
	}

	private void PlaySong(MusicNote[] notes) {
		currentSong = notes;
		phases = new double[notes.Length];

		outputSampleRate = AudioSettings.outputSampleRate;
		songStartTime = AudioSettings.dspTime;

		double songEndOffset = 0f;
		foreach (var note in notes) {
			if (note.Duration + note.StartTime > songEndOffset) {
				songEndOffset = note.Duration + note.StartTime;
			}
		}

		songEndTime = songStartTime + songEndOffset;

		IsPlaying = true;
		audioSource.Play();
	}

	public void StopSong() {
		IsPlaying = false;
		audioSource.Stop();
	}

	private void Update() {
		if (IsPlaying && AudioSettings.dspTime >= songEndTime) {
			audioSource.Stop();
			IsPlaying = false;
			OnSongFinished.Invoke();
		}

		if (IsPlaying) {
			double totalDur = songEndTime - songStartTime;
			double currentDur = AudioSettings.dspTime - songStartTime;
			var totalDurTime = TimeSpan.FromSeconds(totalDur);
			var currentDurTime = TimeSpan.FromSeconds(currentDur);
			var text = $"{currentDurTime.ToString(@"mm\:ss")} | {totalDurTime.ToString(@"mm\:ss")}";
			RelayManagerWindow.SetSongDurationLabel(text);
		}
	}

	private void OnAudioFilterRead(float[] data, int channels) {
		for (int i = 0; i < data.Length; i += channels) {
			double time = AudioSettings.dspTime - songStartTime;

			double output = 0f;
			int numNotes = 0;

            for (int n = 0; n < currentSong.Length; n++) {
                MusicNote note = currentSong[n];

				double noteTime = time - note.StartTime;

                if (noteTime > 0f && noteTime < note.Duration) {
					double amplitude = 0.5f;

					double noteTimeLeft = note.Duration - noteTime;
					if (noteTime < FadeTime) {
						amplitude *= Math.Pow(noteTime / FadeTime, 2);
					} else if (noteTimeLeft < FadeTime) {
						amplitude *= Math.Pow(noteTimeLeft / FadeTime, 2);
					}

					output += Math.Sin(phases[n]) * amplitude;

					phases[n] += (note.Frequency * Math.PI * 2f) / outputSampleRate;

					if (phases[n] > Math.PI * 2.0) {
						phases[n] -= Math.PI * 2.0;
					}

					numNotes++;
				}
			}

			output /= (numNotes == 0 ? 1 : numNotes);

			for (int j = 0; j < channels; j++) {
				data[i + j] = (float)output;
			}
		}
	}
}

