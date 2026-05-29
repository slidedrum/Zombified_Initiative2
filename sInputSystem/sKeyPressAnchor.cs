using System;

namespace SlideDrum.sInputSystem
{
    public class sKeyPressAnchor
    {
        public enum Anchorpoint
        {
            Start,
            Inside,
            End
        }
        public sKeyPressDefinition PressDefinition; // The other key press instance that this is anchored to
        public Anchorpoint OtherPoint; // What point of the other key press is it anchored to
        public Anchorpoint ThisPoint; // What point of the this key press is it anchored to
        public float WindowStart; // What offset of the anchor point does the window start? (can be negative)
        public float WindowEnd; // what offset of the anchor point does the window end? (can be negative)
        public sKeyPressAnchor(sKeyPressDefinition PressDefinition, Anchorpoint ThisPoint, Anchorpoint OtherPoint, float WindowStart, float WindowEnd)
        {
            this.PressDefinition = PressDefinition;
            this.OtherPoint = OtherPoint;
            this.ThisPoint = ThisPoint;
            this.WindowStart = WindowStart;
            this.WindowEnd = WindowEnd;
        }
        public sKeyPressAnchor(sKeyPressAnchor Other)
        {
            this.PressDefinition = Other.PressDefinition;
            this.OtherPoint = Other.OtherPoint;
            this.ThisPoint = Other.ThisPoint;
            this.WindowStart = Other.WindowStart;
            this.WindowEnd = Other.WindowEnd;
        }

        internal bool Matches(sKeyPressRefrence timelineEvent)
        {
            if (!PressDefinition.AnyMatches) // If we don't have anything to anchor to, then it's not a match.
                return false;
            foreach (var candidate in PressDefinition.MatchCandidates)
            {
                if (CandidateMataches(timelineEvent, candidate))
                    return true;
            }
            return false;
        }
        internal bool CandidateMataches(sKeyPressRefrence PressA, sKeyPressRefrence PressB)
        {
            float WindowStartTime = 0; //these will always be overidden because there are only 3 possible Anchorpoints.  but need to tell compiler that it'll never be undefined.
            float WindowEndTime = 0;
            float AnchorStartTime = 0;
            float AnchorEndTime = 0;
            switch (ThisPoint)
            {
                case (Anchorpoint.Start):
                    AnchorStartTime = PressA.Start;
                    AnchorEndTime = PressA.Start;
                    break;
                case (Anchorpoint.End):
                    AnchorStartTime = PressA.End;
                    AnchorEndTime = PressA.End;
                    break;
                case (Anchorpoint.Inside):
                    AnchorStartTime = PressA.Start;
                    AnchorEndTime = PressA.End;
                    break;
            }
            switch (OtherPoint)
            {
                case (Anchorpoint.Start):
                    WindowStartTime = PressB.Start + WindowStart;
                    WindowEndTime = PressB.Start + WindowEnd;
                    break;
                case (Anchorpoint.End):
                    WindowStartTime = PressB.End + WindowStart;
                    WindowEndTime = PressB.End + WindowEnd;
                    break;
                case (Anchorpoint.Inside):
                    WindowStartTime = PressB.Start + WindowStart;
                    WindowEndTime = PressB.End + WindowEnd;
                    break;
            }
            if (AnchorEndTime >= WindowStartTime && AnchorStartTime <= WindowEndTime)
                return true;
            return false;
        }
    }
}