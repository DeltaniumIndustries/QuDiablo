using XRL.UI;

public static class DepthsConfigHandler
{
    public static bool PLACEHOLDER => GetBooleanOption("OptionQuDiabloTest");

    private static bool GetBooleanOption(string optionKey) =>
        Options.GetOption(optionKey).EqualsNoCase("Yes");
}
