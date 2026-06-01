using FluffyUnderware.DevTools.Extensions;
using Player;
using System.Linq;
using TMPro;
using UnityEngine;

namespace BotControl.SmartSelect
{
    internal static class zSmartSelectHud
    {
        public static GameObject TopTextGobject = null;
        public static TextMeshPro TopText = null;
        public static GameObject BottomTextGobject = null;
        public static TextMeshPro BottomText = null;
        private static bool isSetup => TopTextGobject != null;
        public static int staticSize = 10;
        public static int lowerStaticSize = 16;
        public static float lowerFontSize = 10;
        public static float upperStaticSize = 15;
        public static Color defaultColor = new Color(1f, 1f, 1f, 0.25f);
        public static void Setup()
        {
            if (isSetup)
                return;
            GameObject Donor = GameObject.Find("GUI/CellUI_Camera(Clone)/PlayerLayer/MovementRoot/PUI_LocalPlayerStatus_CellUI(Clone)/ShieldBar/");
            GameObject Clone = GameObject.Instantiate(Donor, Donor.transform.parent);
            Clone.transform.Find("ShieldFill Right").Destroy();
            Clone.transform.Find("ShieldFill Left").Destroy();
            TopTextGobject = Clone.transform.Find("ShieldText").gameObject;
            TopTextGobject.name = "SmartSelectText";
            var oldParrent = TopTextGobject.transform.parent;
            TopTextGobject.transform.parent = Donor.transform.parent;
            oldParrent.Destroy();
            TopTextGobject.transform.position = new Vector3(0, -1040f, 1870f);
            TopTextGobject.GetComponent<RectTransform>().sizeDelta = new Vector2(515f, 25f);
            TopText = TopTextGobject.GetComponent<TextMeshPro>();
            TopText.color = defaultColor;
            BottomTextGobject = GameObject.Instantiate(TopTextGobject, TopText.transform.parent);
            BottomTextGobject.transform.position = new(-27f, -1068f, 1870f);
            BottomText = BottomTextGobject.GetComponent<TextMeshPro>();
            BottomText.fontSize = lowerFontSize;
            TopText.fontSize = upperStaticSize;
        }
        public static void Update()
        {
            if (!isSetup)
                return;
            string name = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault()?.Agent.PlayerName ?? "None";
            Pad(ref name,' ');
            string tapName = zSmartSelect.TapPress.CurrentAction?.FriendlyNameShort ?? "";
            ConvertToStaticSize(ref tapName, staticSize, '-');
            Pad(ref tapName, ' ');
            string holdName = zSmartSelect.HoldPress.CurrentAction?.FriendlyNameShort ?? "";
            ConvertToStaticSize(ref holdName, staticSize, '-');
            Pad(ref holdName, ' ');
            string doubleTapName = zSmartSelect.DoubleTapPress.CurrentAction?.FriendlyNameShort ?? "";
            ConvertToStaticSize(ref doubleTapName, staticSize, '-');
            Pad(ref doubleTapName, ' ');
            string tapAndHoldName = zSmartSelect.TapAndHoldPress.CurrentAction?.FriendlyNameShort ?? "";
            ConvertToStaticSize(ref tapAndHoldName, staticSize, '-');
            Pad(ref tapAndHoldName, ' ');
            string tapLabel = "Tap";
            ConvertToStaticSize(ref tapLabel, lowerStaticSize, ' ');
            string holdLabel = "Hold";
            ConvertToStaticSize(ref holdLabel, lowerStaticSize, ' ');
            string doubleTapLabel = "D-Tap";
            ConvertToStaticSize(ref doubleTapLabel, lowerStaticSize, ' ');
            string tapAndHoldLabel = "T-Hold";
            ConvertToStaticSize(ref tapAndHoldLabel, lowerStaticSize, ' ');
            var botAgent = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault()?.Agent;
            if (botAgent != null)
                TopText.color = botAgent.Owner.PlayerColor;
            else
                TopText.color = defaultColor;
            TopText.SetText($"[{tapName}|{holdName}|{doubleTapName}|{tapAndHoldName}]");
            BottomText.SetText($"{tapLabel}   {holdLabel}   {doubleTapLabel}   {tapAndHoldLabel}");
        }
        public static void ConvertToStaticSize(ref string inputString, int size, char fillerChar, bool formattedPadding = true)
        {
            inputString ??= string.Empty;

            // Strip tags to measure visible length only
            int visibleLength = GetVisibleLength(inputString);

            if (visibleLength >= size)
                return;

            int totalPadding = size - visibleLength;
            // Bias toward more padding on the right.
            int leftPadding = totalPadding / 2;
            int rightPadding = totalPadding - leftPadding;

            string leftStr = new string(fillerChar, leftPadding);
            string rightStr = new string(fillerChar, rightPadding);

            if (!formattedPadding)
            {
                // Padding goes outside all formatting — wrap the entire string
                inputString = leftStr + inputString + rightStr;
                return;
            }

            // formattedPadding = true:
            // Left padding goes AFTER any leading tags; right padding goes BEFORE any trailing tags.
            // Tags in the middle of the string stay in place.

            // Find insertion point for left padding: after all leading tags
            int leftInsert = 0;
            while (leftInsert < inputString.Length)
            {
                if (inputString[leftInsert] == '<')
                {
                    int closeIndex = inputString.IndexOf('>', leftInsert);
                    if (closeIndex == -1) break; // malformed tag, stop scanning
                    leftInsert = closeIndex + 1;
                }
                else
                {
                    break;
                }
            }

            // Find insertion point for right padding: before any trailing closing tags
            int rightInsert = inputString.Length;
            while (rightInsert > leftInsert)
            {
                // Walk backwards: skip whitespace then look for a closing '>'
                int searchEnd = rightInsert - 1;
                if (inputString[searchEnd] != '>')
                    break;

                int openIndex = inputString.LastIndexOf('<', searchEnd);
                if (openIndex == -1 || openIndex < leftInsert)
                    break;

                // Only treat it as a trailing closing tag if it starts with '</'
                if (openIndex + 1 < inputString.Length && inputString[openIndex + 1] == '/')
                {
                    rightInsert = openIndex;
                }
                else
                {
                    break;
                }
            }

            inputString = inputString.Substring(0, leftInsert)
                + leftStr
                + inputString.Substring(leftInsert, rightInsert - leftInsert)
                + rightStr
                + inputString.Substring(rightInsert);
        }

        /// <summary>
        /// Returns the visible character count of a string, ignoring Unity rich text / TMPro tags like
        /// &lt;color=#fff&gt;, &lt;b&gt;, &lt;size=20&gt;, &lt;/color&gt;, etc.
        /// </summary>
        private static int GetVisibleLength(string input)
        {
            int count = 0;
            int i = 0;
            while (i < input.Length)
            {
                if (input[i] == '<')
                {
                    int closeIndex = input.IndexOf('>', i);
                    if (closeIndex != -1)
                    {
                        i = closeIndex + 1; // skip the entire tag
                        continue;
                    }
                }
                count++;
                i++;
            }
            return count;
        }
        public static void Pad(ref string inputString, char PadChar)
        {
            inputString = $"{PadChar}{inputString}{PadChar}";
        }
    }
}
