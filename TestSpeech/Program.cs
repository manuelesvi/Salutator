using System.Speech.Synthesis;
using System.Globalization;
using System.Diagnostics;
using System.Reflection.Metadata;

#pragma warning disable CA1416
var synthesizer = new SpeechSynthesizer();

while (true)
{
    if (!PrintMenu(synthesizer)) break;

    Console.Write("Repeat? [Y/N] (Default N): ");
    var repeat = Console.ReadKey();
    if (repeat.Key != ConsoleKey.Y)
        break;

    Console.WriteLine();
    Console.WriteLine();
}

return;

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
    var promptBuilder = GetPromptBuilder(voiceOpt, vInfo, anio);

    // synthesizer.SetOutputToWaveFile("out.wav");
    synthesizer.SetOutputToDefaultAudioDevice();
    synthesizer.Speak(promptBuilder);
    return true;
}

static PromptBuilder GetPromptBuilder(List<(int, VoiceInfo)> voiceOpt, VoiceInfo vInfo, int anio)
{
    var promptBuilder = new PromptBuilder();
    promptBuilder.StartVoice(vInfo.Name);

    string name = GetPersonName(vInfo);
#pragma warning disable
    string salute = vInfo.Culture.Name switch
    {
        "fr-FR" or "fr-CA" => $"Aló {name}, como ça va?",
        "en-US" or "en-GB" => $"Hello {name}, how are you?",
        "es-ES" => $"Hola tío, vaya vaya con que {name}",
        "es-MX" => $"Hola {name}, ¿cómo te va?"
    };
#pragma warning enable

    promptBuilder.AppendText(salute);
    promptBuilder.EndVoice();

    promptBuilder.AppendBreak(PromptBreak.Small);

    promptBuilder.StartStyle(new PromptStyle
    {
        Emphasis = PromptEmphasis.Reduced,
        Rate = PromptRate.Medium,
        Volume = PromptVolume.Loud
    });

    promptBuilder.StartVoice(Constants.esES);
    promptBuilder.AppendText("Muy bien don Quijote, ¡gracias!",
        PromptEmphasis.Moderate);
    promptBuilder.EndVoice();

    promptBuilder.EndStyle();
    promptBuilder.AppendBreak(PromptBreak.Small);

    promptBuilder.StartStyle(new PromptStyle
    {
        Emphasis = PromptEmphasis.None,
        Rate = PromptRate.Fast,
        Volume = PromptVolume.ExtraLoud
    });

    promptBuilder.StartVoice(vInfo);

    promptBuilder.AppendText(GetSalutes(vInfo, anio));

    promptBuilder.EndVoice();
    promptBuilder.EndStyle();

    return promptBuilder;
#pragma warning restore CA1416
}

static string GetSalutes(VoiceInfo vInfo, int anio) => vInfo.Culture.Name switch
{
    "es-ES" => $"¡Feliz Navidad y próspero año {anio} desde España!",
    "es-MX" => $"¡Feliz Navidad y próspero año {anio}!",
    "fr-FR" or "fr-CA" => $"Joyeux Noël et bonne année {anio}",
    "en-US" or "en-GB" or _ => $"Merry Christmas and Happy {anio}",
};

static string GetPersonName(VoiceInfo vInfo)
{
    var name = vInfo.Name.Replace("Microsoft", null);
    name = name.Replace("Desktop", null);
    name = name.Trim();
    return name;
}

public static class Constants
{
    public static CultureInfo esES => CultureInfo.GetCultureInfo("es-ES");
}