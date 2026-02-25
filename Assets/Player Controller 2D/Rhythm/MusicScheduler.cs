//using UnityEngine;

//public class MusicScheduler : MonoBehaviour
//{
//    [SerializeField] private RhythmClock clock;

//    public void PlayClipOnNextBeat(AudioSource source, AudioClip clip)
//    {
//        if (source == null || clip == null || clock == null) return;

//        double dspTime = clock.GetDspTimeToNextBeat();
//        source.clip = clip;
//        source.PlayScheduled(dspTime);
//    }

//    public void PlayClipOnNextBar(AudioSource source, AudioClip clip)
//    {
//        if (source == null || clip == null || clock == null) return;

//        double dspTime = clock.GetDspTimeToNextBar();
//        source.clip = clip;
//        source.PlayScheduled(dspTime);
//    }
//}