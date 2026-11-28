using System.Collections.Generic;
using PianoMasters.Notes;
using UnityEngine;

public class Level : MonoBehaviour
{
    public LoadSong song;
    List<LoadSong.Note> noteList;
    double startTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        noteList = song.CompileToNoteList();
        startTime = AudioSettings.dspTime + 1;
        song.StartPlayback(startTime);
    }

    List<LoadSong.Note> GetNearbyNotes(int prevNotes=5, int nextNotes=5)
    {
        List<LoadSong.Note> output = new();
        double target = AudioSettings.dspTime - startTime;
        int low = 0;
        int high = noteList.Count;
        // use binary search to find the next note
        while(low < high)
        {
            int mid = (low + high) / 2;
            if (noteList[mid].startTime > target) high = mid;
            else low = mid + 1;
        }

        for(int i = prevNotes; i > 0; i--)
        {
            if (low - i >= 0) output.Add(noteList[low - i]);
        }
        for (int i = 0; i < nextNotes; i++)
        {
            if (low + i < noteList.Count) output.Add(noteList[low + i]);
        }

        return output;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
