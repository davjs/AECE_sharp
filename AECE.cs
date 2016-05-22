using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Assets.AECE.AEF;
using UnityEngine;
using Task = System.Threading.Tasks.Task;
using Timer = System.Timers.Timer;

namespace Assets
{
    namespace AECE
    {
        public class Engine : IDisposable
        {
            private readonly Dictionary<string,SongWithAef> _songs;
            private PlaySong _musicPlayer;
            private readonly ConcurrentQueue<AudioEvent> _events = new ConcurrentQueue<AudioEvent>();
            private readonly List<PatternProgression> _patternProgessList = new List<PatternProgression>();
            private readonly List<Timer> _timers = new List<Timer>(); 

            public Engine (IEnumerable<SongWithAef> songs)
            {
                _songs = songs.ToDictionary(x => x.Name,x => x);
            }

            public delegate void PlaySong(string s);

            public void SetMusicPlayer(PlaySong playSongLambda)
            {
                _musicPlayer = playSongLambda;
            }

            public void StartPlaying(string songName)
            {
                var song = _songs[songName];
                _musicPlayer(song.Name);
                var bps = 60.0f / song.Aef.Tempo;
                var instrumentId = 0;
                foreach (var instrument in song.Aef.Instruments)
                {
                    foreach (var pattern in instrument.Patterns)
                    {
                        var timeBetweenHits = bps * (4.0f / pattern.HitsPerBar) * 1000;
                        var offset = pattern.Offset * timeBetweenHits;
                        InitiatePattern(instrumentId, offset, timeBetweenHits, pattern);
                    }
                    instrumentId++;
                }
            }

            public void InitiatePattern(int instrument,float offset,float timeBetweenHits, Pattern pattern)
            {
                var patternProgression = new PatternProgression(pattern, instrument, offset, timeBetweenHits,
                    x => _events.Enqueue(x));
                _patternProgessList.Add(patternProgression);
            }

            public IEnumerable<AudioEvent> PollEvents()
            {
                AudioEvent x;
                while (_events.TryDequeue(out x))
                    yield return x;
            } 

            public class AudioEvent
            {
                public readonly int InstrumentId;
                public readonly PatternKind Kind;
                public readonly int Parameter;

                public AudioEvent(int instrumentId, PatternKind kind, int parameter = 0)
                {
                    InstrumentId = instrumentId;
                    Kind = kind;
                    Parameter = parameter;
                }
            }

            public void Dispose()
            {
                _timers.ForEach(x => x.Stop());
            }
        }
    }
}

namespace Assets.AECE
{
    public enum PatternKind
    {
        Hit,Positional,
        IntensityIncrease
    }

    public class PatternProgression
    {
        private readonly Pattern _pattern;
        private readonly int _instrument;
        private readonly float _timeBetweenHits;
        private readonly Timer _timer;
        private int _scheduledEvent;
        private int _currentEvent;

        public PatternProgression(Pattern pattern, int instrument, float offset, float timeBetweenHits, 
            Action<Engine.AudioEvent> progressConsumer)
        {
            _pattern = pattern;
            _instrument = instrument;
            _timeBetweenHits = timeBetweenHits;
            _timer = new Timer();
            _timer.Elapsed += (sender, args) => progressConsumer(Progress());

            var nextEvent = pattern.CrotchetTillFirstEvent();
            var timeTillFirstHit = offset + nextEvent * timeBetweenHits;
            _scheduledEvent = nextEvent + 1;
            if (timeTillFirstHit < Time.deltaTime)
            {
                progressConsumer(Progress());
            }
            else
                _timer.Interval = timeTillFirstHit;

            _timer.Enabled = true;
        }

        public Engine.AudioEvent Progress()
        {
            _currentEvent = _scheduledEvent;
            _scheduledEvent = ScheduleNextEventAfter(_currentEvent);

            switch (_pattern.Type)
            {
                case "Hit":
                    return new Engine.AudioEvent(_instrument,PatternKind.Hit);
                case "Positional":
                    return new Engine.AudioEvent(_instrument,PatternKind.Positional,_pattern.GetLocation(_currentEvent));
                case "IntensityIncrease":
                    return new Engine.AudioEvent(_instrument,PatternKind.IntensityIncrease);
                default:
                    throw new ArgumentException();
            }
        }

        private int ScheduleNextEventAfter(int currentEvent)
        {
            int hitsToNext;
            int locationOfnext;
            if (_pattern.IndexIsLastHit(currentEvent))
            {
                if (_pattern.ShouldRepeat)
                {
                    var hitsToRestart = _pattern.TotalHits - currentEvent;
                    locationOfnext = _pattern.CrotchetTillFirstEvent() + 1;
                    hitsToNext = hitsToRestart + locationOfnext;
                }
                else
                {
                    _timer.Dispose();
                    return 0;
                }
            }
            else
            {
                locationOfnext = _pattern.GetEventAfter(currentEvent);
                hitsToNext = locationOfnext - currentEvent;
            }

            _timer.Interval = hitsToNext*_timeBetweenHits;
            return locationOfnext;
        }
    }
}