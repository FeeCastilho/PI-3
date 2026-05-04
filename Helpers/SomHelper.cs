using System.Media;

namespace DraftosaurusClient.Helpers;

/// <summary>
/// Sons do jogo. Gera arquivos WAV pequenos no diretorio temporario no
/// primeiro uso (sons curtos sintetizados a sem dependencia de arquivos
/// externos). Toca de forma assincrona para nao travar a UI.
///
/// Sons disponiveis:
///   NovoTurno    a tom curto de notificacao (pingue 880Hz)
///   ColocarDino  a clique grave (200Hz)
///   FimDeJogo    a fanfarra simples (3 tons ascendentes)
///   Erro         a buzz grave dissonante
///
/// Se o usuario tiver arquivos em Resources/sons/{nome}.wav, esses
/// substituem os gerados.
/// </summary>
public static class SomHelper
{
    private static readonly Dictionary<string, string> _arquivos = new();
    private static readonly object _lock = new();
    private static bool _silenciado;

    public static bool Silenciado
    {
        get => _silenciado;
        set => _silenciado = value;
    }

    // A funcao serve para iniciar 'NovoTurno' do programa.
    public static void NovoTurno()    => Tocar("novoturno",  GerarBeep(880, 150));
    // Esta funcao executa a etapa 'ColocarDino' do programa.
    public static void ColocarDino()  => Tocar("colocar",    GerarBeep(220, 80));
    // Esta funcao cuida de iniciar 'FimDeJogo' do programa.
    public static void FimDeJogo()    => Tocar("fim",        GerarFanfarra());
    // A funcao serve para iniciar 'Erro' do programa.
    public static void Erro()         => Tocar("erro",       GerarBeep(120, 200));

    // Esta funcao executa a etapa 'Tocar' do programa.
    private static void Tocar(string nome, byte[] dadosWav)
    {
        if (_silenciado) return;
        try
        {
            string path = ObterArquivo(nome, dadosWav);
            // SoundPlayer.Play A assincrono a nao bloqueia
            var sp = new SoundPlayer(path);
            sp.Play();
        }
        catch
        {
            // Falhas de som nao devem quebrar o jogo
        }
    }

    // Esta funcao cuida de iniciar 'ObterArquivo' do programa.
    private static string ObterArquivo(string nome, byte[] dadosWav)
    {
        lock (_lock)
        {
            // 1. Custom em Resources/sons
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string custom = Path.Combine(baseDir, "Resources", "sons", $"{nome}.wav");
            if (File.Exists(custom)) return custom;

            // 2. Cache em temp
            if (_arquivos.TryGetValue(nome, out var p) && File.Exists(p))
                return p;

            string tmp = Path.Combine(Path.GetTempPath(), $"draft_{nome}.wav");
            File.WriteAllBytes(tmp, dadosWav);
            _arquivos[nome] = tmp;
            return tmp;
        }
    }

    // ============================================================
    // GERACAO PROCEDURAL DE WAV
    // ============================================================

    /// <summary>WAV PCM 16-bit mono 22050Hz com beep simples.</summary>
    private static byte[] GerarBeep(double freq, int duracaoMs)
    {
        int sampleRate = 22050;
        int amostras = sampleRate * duracaoMs / 1000;
        var buf = new short[amostras];
        for (int i = 0; i < amostras; i++)
        {
            double t = (double)i / sampleRate;
            // Envelope de fade-in/out (50ms) para evitar cliques
            double env = 1.0;
            int fade = sampleRate / 20;
            if (i < fade) env = (double)i / fade;
            else if (i > amostras - fade) env = (double)(amostras - i) / fade;

            double s = Math.Sin(2 * Math.PI * freq * t) * env * 0.4;
            buf[i] = (short)(s * short.MaxValue);
        }
        return MontarWav(buf, sampleRate);
    }

    private static byte[] GerarFanfarra()
    {
        int sampleRate = 22050;
        double[] freqs = { 523.25, 659.25, 783.99 }; // C5 E5 G5
        int durMs = 180;
        int total = sampleRate * durMs / 1000 * freqs.Length;
        var buf = new short[total];
        int idx = 0;
        foreach (var f in freqs)
        {
            int amostras = sampleRate * durMs / 1000;
            for (int i = 0; i < amostras; i++)
            {
                double t = (double)i / sampleRate;
                double env = 1.0;
                int fade = sampleRate / 30;
                if (i < fade) env = (double)i / fade;
                else if (i > amostras - fade) env = (double)(amostras - i) / fade;
                double s = Math.Sin(2 * Math.PI * f * t) * env * 0.4;
                if (idx < buf.Length) buf[idx++] = (short)(s * short.MaxValue);
            }
        }
        return MontarWav(buf, sampleRate);
    }

    /// <summary>Monta o cabecalho WAV PCM 16-bit mono.</summary>
    private static byte[] MontarWav(short[] amostras, int sampleRate)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        int bytesData = amostras.Length * 2;

        // RIFF header
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + bytesData);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        // fmt chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);            // tamanho do chunk fmt
        bw.Write((short)1);      // PCM
        bw.Write((short)1);      // 1 canal (mono)
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2); // byte rate
        bw.Write((short)2);      // block align
        bw.Write((short)16);     // bits por amostra
        // data chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(bytesData);
        foreach (var s in amostras)
            bw.Write(s);

        return ms.ToArray();
    }
}

