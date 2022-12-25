using System.Speech.Synthesis;
using System.Globalization;

#pragma warning disable CA1416
var synthesizer = new SpeechSynthesizer();

while (true)
{
    if (!PrintMenu(synthesizer)) break;

    Console.Write("Repeat? [Y/N] (Default N): ");
    
    var repeat = Console.ReadKey();
    if (repeat.Key != ConsoleKey.Y) break;

    Console.WriteLine();
    Console.WriteLine();
}
#pragma warning restore CA1416
return;

static string GetSalutes(VoiceInfo vInfo, int anio) => vInfo.Culture.Name switch
{
    "es-ES" => $"¡Feliz Navidad y próspero año {anio} desde España!",
    "es-MX" => $"¡Feliz Navidad y próspero año {anio}!",
    "fr-FR" or "fr-CA" => $"Joyeux Noël et bonne année {anio}",
    "en-US" or "en-GB" or _ => $"Merry Christmas and Happy {anio}",
};

static PromptBuilder BuildSalutePrompt(VoiceInfo vInfo, int anio)
{
    var promptBuilder = new PromptBuilder();
    promptBuilder.StartVoice(vInfo.Name);

    string name = GetPersonName(vInfo);
#pragma warning disable
    string salute = vInfo.Culture.Name switch
    {
        "fr-FR" or "fr-CA" => $"Aló {name}, como ça va?",
        "en-US" or "en-GB" => $"Hello {name}, how are you?",
        "es-ES" => $"Hola tío, vaya vaya, con que {name}",
        "es-MX" => $"Hola {name}, ¿cómo te va?"
    };
#pragma warning enable

    promptBuilder.AppendText(salute);
    promptBuilder.EndVoice();

    promptBuilder.AppendBreak(PromptBreak.Small);

    promptBuilder.StartVoice(Constants.España);
    promptBuilder.StartStyle(new PromptStyle
    {
        Emphasis = PromptEmphasis.Reduced,
        Rate = PromptRate.Slow,
        Volume = PromptVolume.Loud
    });
    promptBuilder.AppendText("Muy bien don Quijote, ¡gracias!",
        PromptEmphasis.Moderate);
    promptBuilder.EndStyle();
    promptBuilder.EndVoice();

    promptBuilder.AppendBreak(PromptBreak.Small);

    var style = new PromptStyle
    {
        Emphasis = PromptEmphasis.None,
        Rate = PromptRate.NotSet,
        Volume = PromptVolume.ExtraLoud
    };

    var estructura = new LanguageRegion { Culture = vInfo.Culture };
    style.Rate = estructura switch
    {
        { Language: "es", Region: "es-MX" } => PromptRate.Fast,
        { Language: "es" } => PromptRate.Medium,
        { Language: "fr" } => PromptRate.Medium,
        { Language: "en" } => PromptRate.Slow,
        _ => PromptRate.Medium, // default
    };
    promptBuilder.StartVoice(vInfo);
    promptBuilder.StartStyle(style);

    promptBuilder.AppendText(GetSalutes(vInfo, anio));

    promptBuilder.EndStyle();
    promptBuilder.EndVoice();

    return promptBuilder;
}

static bool PrintMenu(SpeechSynthesizer synthesizer)
{
    Console.WriteLine("Select Voice:");
    var voices = synthesizer.GetInstalledVoices();
    int i = 1;
    var voiceOpt = new List<ValueTuple<int, VoiceInfo>>();
    foreach (var v in voices)
    {
        Console.WriteLine($"{i++}.- {GetPersonName(v.VoiceInfo)} [{v.VoiceInfo.Culture.DisplayName}]");
        voiceOpt.Add((i, v.VoiceInfo));
    }

    Console.WriteLine();
    Console.WriteLine("Q.- Quit safely");

    Console.Write("Option: ");

    var line = Console.ReadLine().ToUpper();
    if (line == "Q") return false;

    var opt = Convert.ToInt32(line) - 1;

    Console.Write($"Año [{DateTime.Today.Year + 1}]: ");
    line = Console.ReadLine();
    line = !string.IsNullOrWhiteSpace(line) ? line
        : (DateTime.Today.Year + 1).ToString();

    int anio = Convert.ToInt32(line);
    var vInfo = voiceOpt[opt].Item2;
    var promptBuilder = BuildSalutePrompt(vInfo, anio);

    // synthesizer.SetOutputToWaveFile("out.wav");
    synthesizer.SetOutputToDefaultAudioDevice();

    synthesizer.Speak(promptBuilder);
    return true;
}

static string GetPersonName(VoiceInfo vInfo)
{
    var name = vInfo.Name.Replace("Desktop", string.Empty);
    name = name.Replace("Microsoft", string.Empty);

    name = name.Trim();
    return name;
}

public static class Constants
{
    public static CultureInfo España => CultureInfo.GetCultureInfo("es-ES");
}

public struct LanguageRegion
{
    private CultureInfo cultureInfo;
    public string Region;
    public string Language;
    public CultureInfo Culture
    {
        get => cultureInfo;
        set
        {
            cultureInfo = value;
            Language = cultureInfo.TwoLetterISOLanguageName;
            Region = cultureInfo.Name;
        }
    }
}
