using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TabulatureConverter.Interfaces;

namespace TabulatureConverter.Classes;


public abstract class GenericStringInstrumentTabBuilder : IInstrumentTabBuilder
{
    protected string[] _strings;
    protected int[] _stringOffsets;

    protected GenericStringInstrumentTabBuilder(string[] stringNames, int[] stringOffsets)
    {
        _strings = stringNames;
        _stringOffsets = stringOffsets;
    }

    protected GenericStringInstrumentTabBuilder()
    {
        _strings = new string[] { "e", "B", "G", "D", "A", "E" };
        _stringOffsets = new int[] { 52, 47, 43, 38, 33, 28 };
    }
    
    public virtual string Build(List<MusicalPart> musicalParts, Note lowestNote)
    {
        int transpose = CalculateTranspose(lowestNote);
        
        #region Sort and group musical parts by start
        
        MusicalPart[] sortedMusicalParts = musicalParts.ToArray();
        Array.Sort(sortedMusicalParts, (a, b) => a.Start.CompareTo(b.Start));
        
        var groupsByStart =
            from musicalPart in sortedMusicalParts
            group musicalPart by musicalPart.Start;
        
        #endregion
        
        StringBuilder[] strings = new StringBuilder[_strings.Length];
        for (int i = 0; i < strings.Length; ++i)
        {
            strings[i] = new StringBuilder($"{_strings[i]}|-");
        }

        foreach (var group in groupsByStart)
        {
            bool[] availableStrings = new bool[] { true, true, true, true };
            int longestPart = 0;
            bool isSeparator = false;
            foreach (var musicalPart in group)
            {
                if (musicalPart.Symbols.Count == 1 && musicalPart.Symbols[0] is Technique singleTechnique)
                {
                    // Special case for separator
                    if (singleTechnique.Name == "|")
                    {
                        isSeparator = true;
                        continue;
                    }
                }

                int stringIndex = -1;
                if (musicalPart.LowestNote.GetSemitones() != Int32.MaxValue)
                {
                    // Find the string that the lowest note in the part can be played on
                    stringIndex = NoteFinder.Find(Note.FromSemitones(musicalPart.LowestNote.GetSemitones() + transpose*Constants.TonesInOctave),
                        _stringOffsets, availableStrings).instrumentString;
                }

                if (stringIndex != -1)
                {
                    // Put the whole part on the string
                    availableStrings[stringIndex] = false;
                    foreach (var symbol in musicalPart.Symbols)
                    {
                        if (symbol is Note note)
                        {
                            note.Transpose(transpose*Constants.TonesInOctave);
                            int fret = NoteFinder.Find(note, _stringOffsets, availableStrings, stringIndex).fret;
                            strings[stringIndex].Append($"{fret}");
                        }
                        else if (symbol is Technique technique)
                        {
                            strings[stringIndex].Append($"{technique.Name}");
                        }
                    }
                }
            }
            
            if (isSeparator)
            {
                for (int i = 0; i < strings.Length; ++i)
                {
                    strings[i].Append('|');
                }
            } else
            {
                // Fill the rest of the strings with dashes
                longestPart = strings.OrderByDescending(s => s.Length).First().Length;
                for (int i = 0; i < strings.Length; ++i)
                {
                    strings[i].Append('-', (longestPart - strings[i].Length) + 1);
                }
            }
        }
        
        // Build the tabulature string
        StringBuilder tab = new StringBuilder();
        tab.AppendLine($"Transposed by: {transpose} octaves");
        for (int i = 0; i < strings.Length; ++i)
        {
            if (i == strings.Length - 1)
            {
                tab.Append(strings[i].ToString());
            }
            else
            {
                tab.AppendLine(strings[i].ToString());
            }
        }
        return tab.ToString();
    }
    
    protected virtual int CalculateTranspose(Note lowestNote)
    {
        // Calculate the number of octaves needed to transpose so the tab can be played on the instrument
        int transpose = 0;
        int lowestNoteSemitones = lowestNote.GetSemitones();
        while (lowestNoteSemitones < _stringOffsets.Min())
        {
            lowestNoteSemitones += Constants.TonesInOctave;
            ++transpose;
        }
        return transpose;
    }
}


public class BanjoTabBuilder : GenericStringInstrumentTabBuilder
{
    public BanjoTabBuilder(string[] stringNames, int[] stringOffsets) : base(stringNames, stringOffsets)
    {
    }
    
    public BanjoTabBuilder()
    {
        _strings = new string[] { "d", "B", "G", "D", "g" };
        _stringOffsets = new int[] { 50, 47, 43, 38, 55 };
    }
    
    public override string Build(List<MusicalPart> musicalParts, Note lowestNote)
    {
        int transpose = CalculateTranspose(lowestNote);
        
        #region Sort and group musical parts by start
        
        MusicalPart[] sortedMusicalParts = musicalParts.ToArray();
        Array.Sort(sortedMusicalParts, (a, b) => a.Start.CompareTo(b.Start));
        
        var groupsByStart =
            from musicalPart in sortedMusicalParts
            group musicalPart by musicalPart.Start;
        
        #endregion
        
        StringBuilder[] strings = new StringBuilder[_strings.Length];
        for (int i = 0; i < strings.Length; ++i)
        {
            strings[i] = new StringBuilder($"{_strings[i]}|-");
        }

        foreach (var group in groupsByStart)
        {
            // G string (55) is special on banjo and can only be used for open G, has to be handled separately
            bool[] availableStrings = new bool[] { true, true, true, true, false };
            bool gIsAvailable = true;
            int longestPart = 0;
            bool isSeparator = false;
            foreach (var musicalPart in group)
            {
                if (musicalPart.Symbols.Count == 1 && musicalPart.Symbols[0] is Note singleNote)
                {
                    // Special case for open G on banjo
                    if (singleNote.GetSemitones() + transpose*Constants.TonesInOctave == 55 && gIsAvailable)
                    {
                        strings[4].Append('0');
                        gIsAvailable = false;
                        continue;
                    }
                }
                
                if (musicalPart.Symbols.Count == 1 && musicalPart.Symbols[0] is Technique singleTechnique)
                {
                    // Special case for separator
                    if (singleTechnique.Name == "|")
                    {
                        isSeparator = true;
                        continue;
                    }
                }

                int stringIndex = -1;
                if (musicalPart.LowestNote.GetSemitones() != Int32.MaxValue)
                {
                    // Find the string that the lowest note in the part can be played on
                    stringIndex = NoteFinder.Find(Note.FromSemitones(musicalPart.LowestNote.GetSemitones() + transpose*Constants.TonesInOctave),
                        _stringOffsets, availableStrings).instrumentString;
                }

                if (stringIndex != -1)
                {
                    // Put the whole part on the string
                    availableStrings[stringIndex] = false;
                    foreach (var symbol in musicalPart.Symbols)
                    {
                        if (symbol is Note note)
                        {
                            note.Transpose(transpose*Constants.TonesInOctave);
                            int fret = NoteFinder.Find(note, _stringOffsets, availableStrings, stringIndex).fret;
                            strings[stringIndex].Append($"{fret}");
                        }
                        else if (symbol is Technique technique)
                        {
                            strings[stringIndex].Append($"{technique.Name}");
                        }
                    }
                }
            }
            
            if (isSeparator)
            {
                for (int i = 0; i < strings.Length; ++i)
                {
                    strings[i].Append('|');
                }
            } else
            {
                // Fill the rest of the strings with dashes
                longestPart = strings.OrderByDescending(s => s.Length).First().Length;
                for (int i = 0; i < strings.Length; ++i)
                {
                    strings[i].Append('-', (longestPart - strings[i].Length) + 1);
                }
            }
        }
        
        // Build the tabulature string
        StringBuilder tab = new StringBuilder();
        tab.AppendLine($"Transposed by: {transpose} octaves");
        for (int i = 0; i < strings.Length; ++i)
        {
            if (i == strings.Length - 1)
            {
                tab.Append(strings[i].ToString());
            }
            else
            {
                tab.AppendLine(strings[i].ToString());
            }
        }
        return tab.ToString();
    }
}


public class BassTabBuilder : GenericStringInstrumentTabBuilder
{
    private readonly string[] _strings;
    private readonly int[] _stringOffsets;

    public BassTabBuilder()
    {
        _strings = new string[] { "G", "D", "A", "E" };
        _stringOffsets = new int[] { 31, 26, 21, 16 };
    }
    
    public BassTabBuilder(string[] stringNames, int[] stringOffsets) : base(stringNames, stringOffsets)
    {
    } 
}