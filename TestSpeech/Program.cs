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

return; //exit

static string GetSalutes(VoiceInfo vInfo, int anio) => vInfo.Culture.Name switch
{
    "es-ES" => $"¡Feliz Navidad y próspero año {anio} desde España!",
    "es-MX" => $"¡Feliz Navidad y próspero año {anio}!",
    "fr-FR" or "fr-CA" => $"Joyeux Noël et bonne année {anio}",
    "en-US" or "en-GB" or _ => $"Merry Christmas and Happy {anio}",
};

static string GetFrenchSalute() =>
    (DateTime.Now.Hour >= 15 || DateTime.Now.Hour < 21
    ? "bon soirée" // 3pm-9pm -> tarde(s)
    : DateTime.Now.Hour >= 21 && DateTime.Now.Hour < 4
        ? "bon nuit" // 9pm-4am -> noche(s) o dia(s)
        : "bonjour");

static string ByeSalute(VoiceInfo vInfo, string name) => vInfo.Culture.Name switch
{
    "es-MX" => $"¡Adiós {name}!",
    "es-ES" => $"Anda {name}, hasta pronto!",
    "fr-FR" or "fr-CA" => $"{GetFrenchSalute()} {name}",
    "en-US" or _ => $"Bye {name}, have a wonderful " + ( // en-US by default (_)
        DateTime.Now.Hour < 3 ? "night" :
        DateTime.Now.Hour < 10 ? "morning" +
        "" :
        DateTime.Now.Hour < 17 ? "day" :
        DateTime.Now.Hour < 20 ? "evening" : "night"),
};

static string GetPersonName(VoiceInfo vInfo)
{
    string name = vInfo.Name.Replace("Desktop", string.Empty);
    name = name.Replace("Microsoft", string.Empty);
    name = name.Trim();
    return name;
}

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
        "es-ES" => $"Hola tío, vaya vaya, ¿con que {name}?",
        "es-MX" => $"Hola {name}, ¿cómo te va?"
    };
#pragma warning enable

    promptBuilder.AppendText(salute);
    promptBuilder.EndVoice();

    promptBuilder.AppendBreak(PromptBreak.Small);

    if (vInfo.Culture.Name != "es-ES")
    {
        promptBuilder.StartVoice(Globals.Espania);
    }

    promptBuilder.StartStyle(new PromptStyle
    {
        Emphasis = PromptEmphasis.Reduced,
        Rate = PromptRate.Slow,
        Volume = PromptVolume.Loud
    });
    promptBuilder.AppendText("Muy bien don Quijote, ¡gracias!");
    //promptBuilder.AppendText("Muy bien don Quijote, ¡gracias!",
    //    PromptEmphasis.Moderate);
    promptBuilder.EndStyle();

    if (vInfo.Culture.Name != "es-ES")
    {
        promptBuilder.EndVoice();
    }

    promptBuilder.AppendBreak(PromptBreak.Small);

    var style = new PromptStyle
    {
        Emphasis = PromptEmphasis.None,
        Rate = PromptRate.NotSet,
        Volume = PromptVolume.ExtraLoud
    };

    var estructura = LanguageRegion.Create(vInfo.Culture);
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

    promptBuilder.AppendBreak(PromptBreak.Large);

    promptBuilder.StartStyle(new PromptStyle { Emphasis = PromptEmphasis.Reduced });
    promptBuilder.AppendText(ByeSalute(vInfo, name));
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
        if (v.VoiceInfo.Culture.Name == "es-ES" 
            && Globals.Espania is null)
        {
            Globals.Espania = v.VoiceInfo;
        }
    }

    Console.WriteLine();
    Console.WriteLine("Q.- Quit safely");

    Console.Write("Option: ");

    var line = Console.ReadLine().ToUpper();
    if (line == "Q") return false;

    var opt = Convert.ToInt32(line) - 1;

    Console.Write($"Year [{DateTime.Today.Year + 1}]: ");
    line = Console.ReadLine();
    line = !string.IsNullOrWhiteSpace(line) ? line
        : (DateTime.Today.Year + 1).ToString();

    int anio = Convert.ToInt32(line);
    var vInfo = voiceOpt[opt].Item2;

    var promptBuilder = BuildSalutePrompt(vInfo, anio);

    string filename = string.Format(
        "{0:yyyy}{0:MM}{0:dd}{0:HH}{0:mm}{0:ss}_{1}.wav",
        DateTime.Now,
        Globals.Reproductions++);

    synthesizer.SetOutputToWaveFile(filename);
    synthesizer.Speak(promptBuilder);

    synthesizer.SetOutputToDefaultAudioDevice();
    synthesizer.Speak(promptBuilder);
    return true;
}

static class Globals
{
    public static VoiceInfo Espania { get; set; }
    internal static int Reproductions { get; set; } = 1;
}

struct LanguageRegion
{
    public string Region;
    public string Language;
    public static LanguageRegion Create(CultureInfo cultureInfo) => new LanguageRegion
    {
        Language = cultureInfo.TwoLetterISOLanguageName,
        Region = cultureInfo.Name
    };
}
#pragma warning restore CA1416