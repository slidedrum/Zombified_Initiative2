using System.Collections.Generic;
using UnityEngine;
using static SlideDrum.sInputSystem.sKeyPressAnchor;

namespace SlideDrum.sInputSystem
{
    public class sKeyPressDefinition
    {
        public KeyCode? Key; // They key that's being checked
        public readonly bool Pressed = true; // if we want it to be pressed or unpressed
        public readonly string? Identifier = null; // a debug identifier to make it easier to find this keypress in logs and timelines
        public readonly float MaxDuration = float.MaxValue; // The max time this key can be pressed. (or unpressed if Pressed is false)
        public readonly float MinDuration = 0f; // The min time this key must be pressed. (or unpressed if Pressed is false)
        public readonly bool Strict = false; // if other key presses in this window will make it fail.
        public readonly bool Breaking = false; // if true, when this key press is found the sequenec definition will fail.
        public List<sKeyPressAnchor> Anchors { get; private set; } = new(); // What is this key press anchored to, what time windows must it be pressed in relation to other key presses in the sequence definition.
        public bool HasAnchors => Anchors.Count > 0;
        public HashSet<sKeyPressRefrence> MatchCandidates = new();
        public bool AnyMatches => MatchCandidates.Count > 0;
        public sKeyPressRefrence FirstMatchCandidate             
        { 
            get 
            { // Could optimize this with caching, probably not needed?
                float MatchTime = float.MaxValue;
                sKeyPressRefrence FirstMatch = null;
                foreach (var Match in MatchCandidates)
                {
                    if (MatchTime > Match.Start)
                    {
                        MatchTime = Match.Start;
                        FirstMatch = Match;
                    }
                }
                return FirstMatch;
            }

        }
        public sKeyPressRefrence LastMatchCandidate
        {
            get
            { // Could optimize this with caching, probably not needed?
                float MatchTime = 0f;
                sKeyPressRefrence LastMatch = null;
                foreach (var Match in MatchCandidates)
                {
                    if (MatchTime < Match.End)
                    {
                        MatchTime = Match.End;
                        LastMatch = Match;
                    }
                }
                return LastMatch;
            }

        }
        public sKeyPressDefinition(
            KeyCode? Key,
            bool Pressed,
            List<sKeyPressAnchor>? Anchors = null,
            string? Identifier = null,
            float MaxDuration = float.MaxValue,
            float MinDuration = 0f,
            bool Strict = false,
            bool Breaking = false)
        {
            this.Key = Key;
            this.Pressed = Pressed;
            this.Identifier = Identifier;
            this.MaxDuration = MaxDuration;
            this.MinDuration = MinDuration;
            this.Strict = Strict;
            this.Breaking = Breaking;
            this.Anchors = Anchors ?? new();
        }
        public sKeyPressDefinition AddAnchor(sKeyPressAnchor Anchor)
        {
            Anchors.Add(Anchor);
            return this;
        }
        public sKeyPressDefinition AddAnchor(sKeyPressDefinition Anchor)
        {
            return AddAnchor(new sKeyPressAnchor(Anchor, Anchorpoint.Start, Anchorpoint.End, 0, sInputSystemDefaults.TapThreshold));
        }
        public sKeyPressDefinition AddAnchor(sKeyPressDefinition Anchor, Anchorpoint ThisPoint, Anchorpoint OtherPoint, float start, float end)
        {
            return AddAnchor(new sKeyPressAnchor(Anchor, ThisPoint, OtherPoint, start, end));
        }
        public sKeyPressDefinition AddAnchors(List<sKeyPressAnchor> Anchors)
        {
            foreach (var Anchor in Anchors)
                AddAnchor(Anchor);
            return this;
        }
        public sKeyPressDefinition ResetMatchCandidates()
        {
            MatchCandidates = new();
            return this;
        }
        public bool CheckMatch(sKeyPressRefrence timelineEvent)
        {
            if (HasAnchors)
            {
                bool AnyAnchorDefinitonsMatched = false;
                foreach (var anchor in Anchors)
                {
                    if (anchor.PressDefinition.AnyMatches)
                    {
                        AnyAnchorDefinitonsMatched = true;
                        break;
                    }
                }
                if (!AnyAnchorDefinitonsMatched)
                    return false;
            }
            if (MaxDuration < timelineEvent.Durration)
                return false;
            if (MinDuration > timelineEvent.Durration)
                return false;
            if (timelineEvent.Pressed != Pressed)
                return false;
            if (timelineEvent.Key != Key)
                if(Strict)
                    return true;
                else
                    return false;
            bool AnyAnchorsMatched = !HasAnchors;
            if (!AnyAnchorsMatched) 
            { 
                foreach (var Anchor in Anchors)
                {
                    if (Anchor.Matches(timelineEvent))
                    {
                        AnyAnchorsMatched = true; // We can't break early here because we might have multiple candidates.
                    }
                } 
            }
            if(AnyAnchorsMatched)
                MatchCandidates.Add(timelineEvent);
            return AnyAnchorsMatched && Breaking;
        }
    }
}
