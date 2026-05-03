namespace DraftosaurusClient.Models;

public class Partida
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public DateTime DataCriacao { get; set; }
    /// <summary>A=Aberta, J=Jogando, E=Encerrada</summary>
    public char Status { get; set; }
}

public class Jogador
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public int Pontuacao { get; set; }
}

public class EstadoPartida
{
    /// <summary>J=Jogando, E=Encerrada</summary>
    public char Status { get; set; }
    public int TurnoAtual { get; set; }
    /// <summary>A=Andamento, F=Finalizado</summary>
    public char StatusTurno { get; set; }
    public int IdJogadorComDado { get; set; }
    public string FaceDado { get; set; } = "";
}

public class JogadaTurno
{
    public int IdJogador { get; set; }
    public string CodigoDinossauro { get; set; } = "";
    public string CodigoCercado { get; set; } = "";
}
