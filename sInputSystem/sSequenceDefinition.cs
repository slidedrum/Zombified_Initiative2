using SlideMenu;
using System;
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
        //private int CurrentMatchCount => Presses.Count(press => press.AnyMatches);
        private bool _AllMatched = false; // This is unreliable and must be phased out.  Must check if the sequence is complete instead.
        private string? Identifier = null;
        public Dictionary<sKeyPressDefinition, sKeyPressRefrence> SolvedSolution { get; private set; }
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
        float? MatchStartTimestamp
        {
            get
            {
                return SolvedSolution?[Presses[0]]?.Start;
            }
        }
        float? MatchEndTimestamp
        {
            get
            {
                return SolvedSolution?[Presses[Presses.Length - 1]]?.End;
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
                                      string Identifier = null,
                                      HashSet<KeyCode> ExclusiveOther = null,
                                      HashSet<KeyCode> ExclusiveSelfExcept = null,
                                      bool autoAnchor = true)
        {
            this.Presses = Presses;
            this.Callback = Callback;
            this.Strict = Strict;
            this.RisingEdgeOnly = RisingEdgeOnly;
            this.ExclusivityWindow = ExclusivityWindow;
            this.ExclusiveSelf = ExclusiveSelf;
            this.Identifier = Identifier;
            this.ExclusiveOther = ExclusiveOther ?? new();
            this.ExclusiveSelfExcept = ExclusiveSelfExcept ?? new();
            if (autoAnchor)
                AutoAnchor();
        }
        private void ResetPressMatches()
        {
            foreach (sKeyPressDefinition Press in Presses)
                Press.ResetMatchCandidates();
        }
        private bool CheckSequenceComplete(
            int Index = 0,
            Dictionary<sKeyPressDefinition, sKeyPressRefrence> Solution = null
        )
        {
            if (Solution == null)
                Solution = new();
            // all presses assigned successfully
            if (Index >= Presses.Length)
            {
                SolvedSolution = Solution;
                return true;
            }

            sKeyPressDefinition press = Presses[Index];

            foreach (sKeyPressRefrence candidate in press.MatchCandidates)
            {
                // optional:
                // prevent same timeline event from satisfying multiple presses
                if (Solution.Values.Contains(candidate))
                    continue;

                if (!CandidateFits(press, candidate, Solution))
                    continue;

                // choose
                Solution.Add(press, candidate);

                // recurse
                if (CheckSequenceComplete(Index + 1, Solution))
                    return true;

                // backtrack
                Solution.Remove(press);
            }
            return false;
        }
        public bool MatchedAll(bool recheck = true)
        {
            if (recheck)
                _AllMatched = CheckSequenceComplete();
            return _AllMatched;
        }
        private bool CandidateFits(
            sKeyPressDefinition press,
            sKeyPressRefrence candidate,
            Dictionary<sKeyPressDefinition, sKeyPressRefrence> solution
        )
        {
            foreach (var anchor in press.Anchors)
            {
                // anchor target not solved yet
                // can't validate yet
                if (!solution.TryGetValue(anchor.PressDefinition, out var other))
                    continue;

                // validate THIS specific pair
                if (!anchor.CandidateMataches(candidate, other))
                    return false;
            }
            return true;
        }
        private bool ContainsInputEvent(sInputEvent Evnt)
        {
            foreach (var KeyPress in SolvedSolution.Values)
            {
                if (KeyPress.ContainsEvent(Evnt))
                {
                    return true;
                }
            }
            return false;
        }
        public bool Matches(List<sKeyPressRefrence> Refrences)
        {
            // Loop through all KeyPresses in the timeline
            // Loop through all presses in the sequence definition, and check if they match the timeline event.
            // Then repeat untill we either didn't find any new matches, or we have matched everything.
            // Then check if the exclusivity window has passed before completeing the match.
            // Check for any breaking key presses after the match, and if we find any, return false.
            // Only if all of that passes do we return true.

            ResetPressMatches();
            int PreviousMatchCount = 0;
            int CurrentMatchCount = 0;
            do
            { // Keep going untill we don't find any new matches.
                PreviousMatchCount = CurrentMatchCount; // This might be bad, we should track new matches instead?
                for (int i = 0; i < Refrences.Count; i++)
                {
                    sKeyPressRefrence TimelineEvent = Refrences[i];
                    for (int j = Presses.Length - 1; j >= 0; j--)
                    {
                        sKeyPressDefinition Press = Presses[j];
                        if (Press.CheckMatch(TimelineEvent, out bool MatchFound))// returns true if should break the match.  Automatically adds any candiates to it's own list to update Press.Matched
                        {
                            return MatchedTail(false);
                        }
                        if (MatchFound)
                        {
                            CurrentMatchCount++;
                            if (MatchedAll(true))
                                break;
                        }
                    }
                    if (MatchedAll())
                        break;
                }
            } while (!MatchedAll() && PreviousMatchCount < CurrentMatchCount);
            if (MatchedAll())
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
                    if (ContainsInputEvent(TimelineEvent))
                        continue;
                    if (BreakList.Contains(TimelineEvent.Key))
                        return MatchedTail(false);
                }

                if (RisingEdgeOnly) // handle differently when we're in rising edge only mode.
                {
                    if (!PreviouslyMatched)// is this a rising edge?
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
        public void AutoAnchor()
        {
            if (Presses.Length > 1)
            {
                for (int i = 1; i < Presses.Length; i++)
                {
                    Presses[i].AddAnchor(Presses[i - 1]);
                }
            }
        }
        public override string ToString()
        {
            if (Identifier != "")
                return Identifier;
            string ret = "";
            foreach (var Press in Presses)
            {
                ret += $"-{Press}";
            }
            return ret;
        }
    }
}