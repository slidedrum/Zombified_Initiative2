using SlideMenu;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public class sSequenceDefinition
    {
        public readonly sKeyPressDefinition[] Presses; // un ordered list, since the key presses will be matched based on their anchors, not their order in this list.
        public FlexibleMethodDefinition Callback; // the method that will be called when this sequence is matched.
        public readonly bool Strict; // Should any other key presses not part of this sequence break the sequence?
        public readonly bool RisingEdgeOnly; // Should this match only trigger on the rising edge of the sequence match? (ie, only trigger when the sequence goes from not matched to matched, not every frame that the sequence is matched)
        public readonly float ExclusivityWindow = 0f; // How long to wait before the sequence is considered matched.
        public readonly bool ExclusiveSelf = true; // Should the KeyCodes in this sequence break this sequence?
        public readonly HashSet<KeyCode> ExclusiveOther = new(); // What other KeyCodes should break this sequence?
        public readonly HashSet<KeyCode> ExclusiveSelfExcept = new(); // Should any KeyCodes in this sequence NOT break it?
        private bool PreviouslyMatched = false; // used for rising edge only, to prevent multiple triggers from the same sequence match
        private bool Matched = false; // used for rising edge only, to prevent multiple triggers from the same sequence match
        private int CurrentMatchCount => Presses.Count(press => press.Matched);
        private bool MatchedAll => CurrentMatchCount == Presses.Length;
        private HashSet<KeyCode> _BreakList;
        public HashSet<KeyCode> BreakList 
        { 
            get 
            {
                if (_BreakList == null)
                {
                    _BreakList = new HashSet<KeyCode>(ExclusiveOther);
                    _BreakList.UnionWith(KeyCodes);
                    foreach (KeyCode element in ExclusiveSelfExcept)
                        _BreakList.Remove(element);
                }
                return _BreakList;
            } 
        }
        public sKeyPressDefinition FirstMatch 
        { 
            get 
            { // Could optimize this with caching, probably not needed?
                float FirstMatchTime = float.MaxValue;
                sKeyPressDefinition _FirstMatch = null;
                foreach (var Press in Presses)
                {
                    if (Press.Matched)
                    {
                        var FirstMatchCandidate = Press.FirstMatchCandidate;
                        if (FirstMatchTime > FirstMatchCandidate.Start)
                        {
                            FirstMatchTime = FirstMatchCandidate.Start;
                            _FirstMatch = Press;
                        }
                    }
                }
                return _FirstMatch;
            } 
        }
        public sKeyPressDefinition LastMatch
        {
            get
            { // Could optimize this with caching, probably not needed?
                float LastMatchTime = 0f;
                sKeyPressDefinition _LastMatch = null;
                foreach (var Press in Presses)
                {
                    if (Press.Matched)
                    {
                        var LastMatchCandidate = Press.LastMatchCandidate;
                        if (LastMatchTime < LastMatchCandidate.End)
                        {
                            LastMatchTime = LastMatchCandidate.End;
                            _LastMatch = Press;
                        }
                    }
                }
                return _LastMatch;
            }
        }
        float? MatchStartTimestamp 
        { 
            get 
            {
                sKeyPressDefinition _FirstMatch = FirstMatch;
                if (_FirstMatch == null)
                    return null;
                return _FirstMatch.FirstMatchCandidate.Start;
            } 
        }
        float? MatchEndTimestamp
        {
            get
            {
                sKeyPressDefinition _LastMatch = LastMatch;
                if (_LastMatch == null)
                    return null;
                return _LastMatch.LastMatchCandidate.End;
            }
        }
        private HashSet<KeyCode> _KeyCodes;
        public HashSet<KeyCode> KeyCodes
        {
            get
            {
                if (_KeyCodes == null)
                {
                    _KeyCodes = new();
                    foreach (var keyPress in Presses)
                        if (keyPress.Key != null)
                            _KeyCodes.Add((KeyCode)keyPress.Key);
                }
                return _KeyCodes;
            }
        }
        public sSequenceDefinition(sKeyPressDefinition[] Presses,
                                      FlexibleMethodDefinition Callback,
                                      bool Strict = false,
                                      bool RisingEdgeOnly = true,
                                      float ExclusivityWindow = 0f,
                                      bool ExclusiveSelf = true,
                                      HashSet<KeyCode> ExclusiveOther = null,
                                      HashSet<KeyCode> ExclusiveSelfExcept = null)
        {
            this.Presses = Presses;
            this.Callback = Callback;
            this.Strict = Strict;
            this.RisingEdgeOnly = RisingEdgeOnly;
            this.ExclusivityWindow = ExclusivityWindow;
            this.ExclusiveSelf = ExclusiveSelf;
            this.ExclusiveOther = ExclusiveOther ?? new();
            this.ExclusiveSelfExcept = ExclusiveSelfExcept ?? new();
        }
        public sSequenceDefinition(sKeyPressDefinition Press,
                                        FlexibleMethodDefinition Callback,
                                        bool Strict = false,
                                        bool RisingEdgeOnly = true,
                                        float ExclusivityWindow = 0f,
                                        bool ExclusiveSelf = true,
                                        HashSet<KeyCode> ExclusiveOther = null,
                                        HashSet<KeyCode> ExclusiveSelfExcept = null)
                                        : this(
                                            new[] { Press },
                                            Callback,
                                            Strict,
                                            RisingEdgeOnly,
                                            ExclusivityWindow,
                                            ExclusiveSelf,
                                            ExclusiveOther,
                                            ExclusiveSelfExcept)
        {

        }
        private void ResetPressMatches()
        {
            foreach (sKeyPressDefinition Press in Presses)
                Press.ResetMatchCandidates();
        }
        public bool Matches()
        {
            // Loop through all KeyPresses in the timeline
            // Loop through all presses in the sequence definition, and check if they match the timeline event.
            // Then repeat untill we either didn't find any new matches, or we have matched everything.
            // Then check if the exclusivity window has passed before completeing the match.
            // Check for any breaking key presses after the match, and if we find any, return false.
            // Only if all of that passes do we return true.

            ResetPressMatches();
            List<sKeyPressRefrence> Refrences = new(sTimeline.KeyPressRefrences); // we want to make our own clone so we don't rebuild the list every time we check it.
            int PreviousMatchCount;
            
            do
            { // Keep going untill we don't find any new matches.
                PreviousMatchCount = CurrentMatchCount;
                for (int i = 0; i < Refrences.Count; i++)
                {
                    sKeyPressRefrence TimelineEvent = Refrences[i];
                    for (int j = Presses.Length - 1; j >= 0; j--)
                    {
                        sKeyPressDefinition Press = Presses[j];
                        if (Press.CheckMatch(TimelineEvent))// returns true if should break the match.  Automatically adds any candiates to it's own list to update Press.Matched
                        {
                            return MatchedTail(false);
                        }
                        if (MatchedAll)
                            break;
                    }
                    if (MatchedAll)
                        break;
                }
            } while (!MatchedAll && PreviousMatchCount < CurrentMatchCount);
            if (MatchedAll)
            {
                if (MatchEndTimestamp + ExclusivityWindow < Time.time)
                {
                    return MatchedTail(false);
                }
                for (int i = 0; i < sTimeline.Sequence.Count; i++)
                {
                    var TimelineEvent = sTimeline.Sequence[i];
                    if (TimelineEvent.Time < (Strict ? MatchStartTimestamp : MatchEndTimestamp))
                        break;
                    // TODO CRITICAL Need to check if TimelineEvent is part of the matched sequence
                    if (BreakList.Contains(TimelineEvent.Key))
                        return MatchedTail(false);
                }

                if (RisingEdgeOnly) // handle differently when we're in rising edge only mode.
                {
                    bool RisingEdge = Matched && !PreviouslyMatched; // is this a rising edge?
                    if (RisingEdge)
                    {
                        return MatchedTail(true);  // continue as normal.
                    }
                    PreviouslyMatched = Matched; // set previously matched
                    Matched = true; // because we're still matched!
                    return false; // but don't actually return true. because we should not invoke.
                }
                return MatchedTail(true);
            }
            return MatchedTail(false);
        }
        private bool MatchedTail(bool Matched)
        {
            this.PreviouslyMatched = Matched;
            this.Matched = Matched;
            return Matched;
        }
        public void SetKeyCode(KeyCode Key)
        {
            foreach (var Press in Presses)
            {
                Press.Key = Key;
            }
        }
    }
}
