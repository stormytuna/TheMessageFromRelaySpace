using System;
using UnityEngine;
using TRFDS.DataStructures;
using UnityEngine.Events;
using TRFDS.UI;
using TRFDS.Helpers;

namespace TRFDS.MonoBehaviours;

public class SongPlayer : MonoBehaviour
{ 
	public static UnityEvent OnSongFinished = new();

	const double FadeTime = 0.01;

	public static SongPlayer Instance;
	public static bool IsPlaying;

	private AudioSource audioSource;

	private MusicNote[] currentSong;
	private double volume;
	private int samplePosition;
	private double[] phases;
	private int outputSampleRate;
	private double songStartTime;
	private double songEndTime;

	private VolumeManager volumeManager;

	private void Awake() {
		Instance = this;
		audioSource = gameObject.AddComponent<AudioSource>();
		volumeManager = UnityHelpers.FindSingleInstanceObject<VolumeManager>();
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
		volume = volumeManager.sfxUI.level / 100f;
		samplePosition = 0;
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
		MusicManager.Instance.PauseNormalMusic();
		audioSource.Play();
	}

	public void StopSong() {
		IsPlaying = false;
		audioSource.Stop();
		MusicManager.Instance.ResumeNormalMusic();
	}

	private void Update() {
		if (IsPlaying && AudioSettings.dspTime >= songEndTime) {
			audioSource.Stop();
			IsPlaying = false;
			OnSongFinished.Invoke();
			MusicManager.Instance.ResumeNormalMusic();
		}

		if (IsPlaying) {
			double totalDur = songEndTime - songStartTime;
			double currentDur = AudioSettings.dspTime - songStartTime;
			var totalDurTime = TimeSpan.FromSeconds(totalDur);
			var currentDurTime = TimeSpan.FromSeconds(currentDur);
			var text = $"{currentDurTime.ToString(@"m\:ss")}|{totalDurTime.ToString(@"m\:ss")}";
			RelayManagerWindow.SetSongDurationLabel(text);
		}
	}

	private void OnAudioFilterRead(float[] data, int channels) {
		if (!IsPlaying) {
			return;
		}

		for (int i = 0; i < data.Length; i += channels) {
			double time = (double)samplePosition / outputSampleRate;
			double output = 0.0;

			for (int n = 0; n < currentSong.Length; n++) {
				MusicNote note = currentSong[n];
				double noteTime = time - note.StartTime;

				if (noteTime >= 0.0 && noteTime < note.Duration) {
					double amplitude = 0.5;

					if (FadeTime > 0.0) {
						double fadeIn = Math.Min(noteTime / FadeTime, 1.0);
						double fadeOut = Math.Min((note.Duration - noteTime) / FadeTime, 1.0);

						amplitude *= fadeIn * fadeIn;
						amplitude *= fadeOut * fadeOut;
					}

					output += Math.Sin(phases[n]) * amplitude;

					phases[n] += note.Frequency * Math.PI * 2.0 / outputSampleRate;

					if (phases[n] >= Math.PI * 2.0) {
						phases[n] -= Math.PI * 2.0;
					}
				}
			}

			output *= 0.2 * volume;

			for (int j = 0; j < channels; j++) {
				data[i + j] += (float)output;
			}

			samplePosition++;
		}
	}
}

