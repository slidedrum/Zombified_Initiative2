using AK;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using CellMenu;
using HarmonyLib;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CollisionRundown.Features.HUDs;
internal static class InGameTitle
{
    public static PUI_InteractionPrompt Prompt;
    private readonly static Regex s_StripRichTextRegex = new("\\<.+?\\>");

    private static int s_SmallTitleLength;
    private static int s_TitleLength;

    public static void DisplayDefault()
    {
        Prompt.m_headerText.maxVisibleCharacters = 0;

        Prompt.SetVisible(true);
        CoroutineManager.StartCoroutine(DoDefaultDisplay().WrapToIl2Cpp());
    }

    public static void SetTitle(string smallTitle, string title)
    {
        var smallTitleClean = s_StripRichTextRegex.Replace(smallTitle, "");
        var titleClean = s_StripRichTextRegex.Replace(title, "");

        SetLength(smallTitleClean.Length, titleClean.Length);
        SetText($"{smallTitle}\n<b><size=200%>{title}");
    }

    public static void SetLength(int smallTitleLength, int titleLength)
    {
        s_SmallTitleLength = smallTitleLength;
        s_TitleLength = titleLength;
    }

    private static IEnumerator DoDefaultDisplay()
    {
        Prompt.m_headerText.alpha = 1.0f;
        Prompt.m_headerText.maxVisibleCharacters = 0;
        Prompt.PlayIntro();

        CM_PageBase.PostSound(EVENTS.HUD_SCAN_COMPLETE_INDICATION, "Display IGT");

        yield return new WaitForSeconds(0.33f);

        int visibleCount = 0;
        while (visibleCount <= s_SmallTitleLength)
        {
            visibleCount++;
            Prompt.m_headerText.maxVisibleCharacters = visibleCount;
            CM_PageBase.PostSound(EVENTS.HUD_EXIT_SCAN_INFO_TEXT_DISAPPEAR, "Type IGT");
            yield return new WaitForSeconds(0.065f);
        }

        yield return new WaitForSeconds(1.0f);

        while (visibleCount <= s_TitleLength + s_SmallTitleLength + 1) // Plus one for LineBreak char
        {
            visibleCount++;
            Prompt.m_headerText.maxVisibleCharacters = visibleCount;
            CM_PageBase.PostSound(EVENTS.HUD_EXIT_SCAN_INFO_TEXT_DISAPPEAR, "Type IGT");
            yield return new WaitForSeconds(0.065f);
        }

        yield return new WaitForSeconds(6.5f);

        CM_PageBase.PostSound(EVENTS.HUD_EXIT_SCAN_INFO_TEXT_APPEAR, "Hide IGT");
        Prompt.PlayIntro();

        yield return new WaitForSeconds(0.33f);

        Prompt.SetVisible(false);
    }

    private static void SetText(string rawText)
    {
        Prompt.m_headerText.SetText(rawText);
    }

    //::0<size=75%>x</size>C001::
    //<b><size=300%>CLICHÉ
}
[HarmonyPatch(typeof(InteractionGuiLayer), nameof(InteractionGuiLayer.Setup))]
internal static class Patch_InteractionLayer_Setup
{
    static void Postfix(InteractionGuiLayer __instance)
    {
        var igtPrompt = __instance.AddRectComp("Gui/Player/PUI_InteractionPrompt_CellUI", GuiAnchor.MidCenter, new Vector2(0f, 258f), null).Cast<PUI_InteractionPrompt>();

        igtPrompt.m_headerText.color = Color.white;
        igtPrompt.m_headerText.fontSize = 24.5f;
        igtPrompt.m_headerText.fontSizeMin = 24.5f;
        igtPrompt.m_headerText.fontSizeMax = 24.5f;

        igtPrompt.m_headerText.rectTransform.sizeDelta = new Vector2(120.0f, -10.0f);

        igtPrompt.m_whiteBox.transform.localPosition = new Vector3(0.0f, -22.8f, 0.0f);
        igtPrompt.m_whiteBox.transform.localScale = new Vector3(1.0f, 1.35f, 1.0f);

        igtPrompt.m_whiteBoxWide.transform.localPosition = new Vector3(0.0f, -22.8f, 0.0f);
        igtPrompt.m_whiteBoxWide.transform.localScale = new Vector3(1.0f, 1.5f, 1.0f);
        igtPrompt.SetTimerFill(0.0f);
        igtPrompt.SetVisible(false);

        InGameTitle.Prompt = igtPrompt;
        InGameTitle.SetTitle("Small debug title here", "Beeeeg debug title here");
    }
}