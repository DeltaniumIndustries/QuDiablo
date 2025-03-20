using XRL.UI;

public static class DepthsConfigHandler
{
    public static readonly bool LOGGING_HARMONY_BPSC = GetBooleanOption("OptionLaurusHarmonyLoggingBPSC");
    public static readonly bool LOGGING_HARMONY_RENDER = GetBooleanOption("OptionLaurusHarmonyLoggingRender");

    private static bool GetBooleanOption(string optionKey) =>
        Options.GetOption(optionKey).EqualsNoCase("Yes");
}
