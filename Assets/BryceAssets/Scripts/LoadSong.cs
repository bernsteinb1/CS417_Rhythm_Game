using UnityEngine;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Unity.VisualScripting;


public class LoadSong : MonoBehaviour
{
    public struct Note
    {
        public double startTime;
        public double holdTime;
        public int audioIdx;
    };

    public struct Measure
    {
        public List<Note> notes;
        public double tempo;
        public int beats;
        public int subdivision;

        public Measure(Measure prev)
        {
            notes = new();
            tempo = prev.tempo;
            beats = prev.beats;
            subdivision = prev.subdivision;
        }
    }

    static JsonDocumentOptions options = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public string SongFile;

    public Piano sounds;
    List<Measure> measures = new();
    List<int> measureOrder = new();
    double nextMeasureStartTime;
    int currentMeasure = 0;
    double offset;
    string songPath;
    bool playing;
    
    void Awake()
    {
        string jsoncContent = File.ReadAllText(SongFile);
        var config = JsonDocument.Parse(jsoncContent, options);
        var dict = config.RootElement;

        JsonElement jsonMeasures = dict.GetProperty("measures");

        Measure prevMeasure = new()
        {
            tempo = 60.0,
            beats = 4,
            subdivision = 1
        };

        foreach (JsonElement measure in jsonMeasures.EnumerateArray())
        {
            Measure m = new(prevMeasure);
            ReadMeasure(ref m, measure);
            prevMeasure = m;
        }

        songPath = dict.GetProperty("filepath").GetString();
        offset = dict.GetProperty("offset").GetDouble();

        Debug.Log("JSON File Loaded.");
    }

    public void StartPlayback(double startTime)
    {
        if (measures.Count == 0) return;

        playing = true;
        AudioSource song = sounds.gameObject.transform.AddComponent<AudioSource>();
        song.volume = 0.2f;
        song.clip = Resources.Load<AudioClip>($"Songs/{songPath}");
        if (offset >= 0)
            song.PlayScheduled(startTime + offset);
        else
        {
            // we want to start somewhere in the song file
            song.timeSamples = (int) (-offset * song.clip.frequency);
            song.PlayScheduled(startTime + offset);
        }
        PlayMeasure(startTime);
        currentMeasure++;
        startTime += measures[0].beats * 60.0 / measures[0].tempo;
        nextMeasureStartTime = startTime;
        if (measureOrder.Count > 1) {
            PlayMeasure(startTime);
            currentMeasure++;
        }
    }
    
    void PlayMeasure(double startTime)
    {
        Measure m = measures[measureOrder[currentMeasure]];
        foreach (Note n in m.notes)
        {
            AudioSource sound = Instantiate(sounds.keys[n.audioIdx], sounds.transform);
            sound.PlayScheduled(startTime + n.startTime);
            sound.SetScheduledEndTime(startTime + n.startTime + n.holdTime);
            Destroy(sound.gameObject, (float) (startTime + n.startTime + n.holdTime - AudioSettings.dspTime + 0.1));
        }
    }

    void ReadMeasure(ref Measure m, JsonElement data)
    {
        if (data.TryGetProperty("meta", out JsonElement metadata)) 
        {
            if (metadata.TryGetProperty("tempo", out JsonElement tempoEl) && tempoEl.TryGetDouble(out double newTempo))
                m.tempo = newTempo;

            if (metadata.TryGetProperty("beats", out JsonElement beatsEl) && beatsEl.TryGetInt32(out int newBeats))
                m.beats = newBeats;

            if (metadata.TryGetProperty("subdivision", out JsonElement subdivEl) && subdivEl.TryGetInt32(out int newSubdivision))
                m.subdivision = newSubdivision;
        }

        if (data.TryGetProperty("notes", out JsonElement notes))
        {
            foreach (JsonElement note in notes.EnumerateArray())
            {
                Note n;
                int holdBeats = note.TryGetProperty("hold", out JsonElement holdLengthEl) ? holdLengthEl.GetInt32() : 1;
                n.holdTime = holdBeats * 60.0 / m.tempo / m.subdivision;
                n.startTime = note.GetProperty("start").GetInt32() * 60.0 / m.tempo / m.subdivision;
                n.audioIdx = ParseNote(note.GetProperty("pitch").GetString());
                m.notes.Add(n);
            }
            // m.notes.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        }
        if (data.TryGetProperty("repeat", out JsonElement repeat))
        {
            for(int i = repeat.GetProperty("start").GetInt32(); i < repeat.GetProperty("end").GetInt32(); i++)
            {
                measureOrder.Add(i);
            }
        }
        else
        {
            measures.Add(m);
            measureOrder.Add(measures.Count - 1);
        }
    }

    static int ParseNote(string note)
    {
        return note switch
        {
            "C3" => 0,
            "C#3" or "Db3" => 1,
            "D3" => 2,
            "Eb3" or "D#3" => 3,
            "E3" or "Fb3" => 4,
            "F3" or "E#3" => 5,
            "F#3" or "Gb3" => 6,
            "G3" => 7,
            "G#3" or "Ab3" => 8,
            "A3" => 9,
            "A#3" or "Bb3" => 10,
            "B3" or "Cb4" => 11,
            "C4" or "B#3" => 12,
            "C#4" or "Db4" => 13,
            "D4" => 14,
            "Eb4" or "D#4" => 15,
            "E4" or "Fb4" => 16,
            "F4" or "E#4" => 17,
            "F#4" or "Gb4" => 18,
            "G4" => 19,
            "G#4" or "Ab4" => 20,
            "A4" => 21,
            "A#4" or "Bb4" => 22,
            "B4" or "Cb5" => 23,
            "C5" or "B#4" => 24,
            _ => 12,
        };
    }

    void Update()
    {
        if (!playing || AudioSettings.dspTime < nextMeasureStartTime) return;

        if (currentMeasure >= measureOrder.Count)
        {
            playing = false;
            return;
        }
        nextMeasureStartTime += measures[measureOrder[currentMeasure - 1]].beats * 60.0 / measures[measureOrder[currentMeasure - 1]].tempo;
        PlayMeasure(nextMeasureStartTime);
        currentMeasure++;
    }

    public List<Note> CompileToNoteList()
    {
        List<Note> output = new();
        double timestamp = 0;
        for (int i = 0; i < measureOrder.Count; i++)
        {
            Measure m = measures[measureOrder[i]];
            List<Note> notes = m.notes;
            for (int j = 0; j < notes.Count; j++)
            {
                Note n;
                n.startTime = timestamp + notes[j].startTime;
                n.holdTime = notes[j].holdTime;
                n.audioIdx = notes[j].audioIdx;
                output.Add(n);
            }
            timestamp += 60.0 / m.tempo * m.beats;
        }
        return output;
    }
}
