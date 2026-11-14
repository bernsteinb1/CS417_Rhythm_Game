using UnityEngine;
using System.IO;
using System.Text.Json;


public class LoadSong : MonoBehaviour
{
    public struct Note
    {
        public double startTime;
        public double holdTime;
        public int audioIdx;
    };

    static JsonDocumentOptions options = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public string SongFile;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string jsoncContent = File.ReadAllText(SongFile);
        var config = JsonDocument.Parse(jsoncContent, options);
        var dict = config.RootElement;

        JsonElement measures = dict.GetProperty("measures");

        double timestamp = 0.0;

        double tempo = 60.0;
        int beats = 4;
        int subdivision = 1;


        foreach (JsonElement measure in measures.EnumerateArray())
        {
            if (measure.TryGetProperty("meta", out JsonElement metadata)) {
                if (metadata.TryGetProperty("tempo", out JsonElement tempoEl) && tempoEl.TryGetDouble(out double newTempo))
                    tempo = newTempo;

                if (metadata.TryGetProperty("beats", out JsonElement beatsEl) && beatsEl.TryGetInt32(out int newBeats))
                    beats = newBeats;

                if (metadata.TryGetProperty("subdivision", out JsonElement subdivEl) && subdivEl.TryGetInt32(out int newSubdivision))
                    subdivision = newSubdivision;
            }

            if (measure.TryGetProperty("notes", out JsonElement notes))
            {
                foreach (JsonElement note in notes.EnumerateArray())
                {
                    Note n;
                    int holdBeats = note.TryGetProperty("hold", out JsonElement holdLengthEl) ? holdLengthEl.GetInt32() : 1;
                    n.holdTime = holdBeats * (((double) beats) / subdivision) * tempo / 60;
                    n.startTime = timestamp + note.GetProperty("start").GetInt32() * (((double) beats) / subdivision) * tempo / 60;
                    n.audioIdx = ParseNote(note.GetProperty("pitch").GetString());
                }
            }

            timestamp += tempo / 60 * beats / subdivision;
        }
        Debug.Log("Done");
    }

    static int ParseNote(string note)
    {
        switch (note)
        {
            case "C3":
                return 0;
            case "C#3" or "Db3":
                return 1;
            case "D3":
                return 2;
            case "Eb3" or "D#3":
                return 3;
            case "E3" or "Fb3":
                return 4;
            case "F3" or "E#3":
                return 5;
            case "F#3" or "Gb3":
                return 6;
            case "G3":
                return 7;
            case "G#3" or "Ab3":
                return 8;
            case "A3":
                return 9;
            case "A#3" or "Bb3":
                return 10;
            case "B3" or "Cb4":
                return 11;
            case "C4" or "B#3":
                return 12;
            case "C#4" or "Db4":
                return 13;
            case "D4":
                return 14;
            case "Eb4" or "D#4":
                return 15;
            case "E4" or "Fb4":
                return 16;
            case "F4" or "E#4":
                return 17;
            case "F#4" or "Gb4":
                return 18;
            case "G4":
                return 19;
            case "G#4" or "Ab4":
                return 20;
            case "A4":
                return 21;
            case "A#4" or "Bb4":
                return 22;
            case "B4" or "Cb5":
                return 23;
            case "C5" or "B#4":
                return 24;
            default:
                return 12;
        }
    }
}
