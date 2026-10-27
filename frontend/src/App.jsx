
import { useEffect, useState } from "react";

const API = "http://localhost:5072/api/habitos";

// Identificação anônima deste navegador
function obterVisitanteId() {
  let id = localStorage.getItem("dailyxp_visitante_id");

  if (!id) {
    id = crypto.randomUUID();
    localStorage.setItem("dailyxp_visitante_id", id);
  }

  return id;
}

const visitanteId = obterVisitanteId();

const headersVisitante = {
  "X-Visitante-Id": visitanteId
};

function App() {
  const [habitos, setHabitos] = useState([]);
  const [nome, setNome] = useState("");
  const [erro, setErro] = useState("");
  const [carregando, setCarregando] = useState(true);

  async function carregarHabitos() {
    try {
      const resposta = await fetch(API, {
        headers: headersVisitante
      });

      if (!resposta.ok) {
        throw new Error("Erro ao consultar hábitos");
      }

      const dados = await resposta.json();
      setHabitos(dados);
      setErro("");
    } catch {
      setErro("Não foi possível conectar à API.");
    } finally {
      setCarregando(false);
    }
  }

  useEffect(function () {
    carregarHabitos();
  }, []);

  async function adicionarHabito(evento) {
    evento.preventDefault();

    if (nome.trim() === "") return;

    try {
      const resposta = await fetch(API, {
        method: "POST",
        headers: {
          ...headersVisitante,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ nome: nome.trim() })
      });

      if (!resposta.ok) throw new Error();

      setNome("");
      await carregarHabitos();
    } catch {
      setErro("Não foi possível adicionar o hábito.");
    }
  }

  async function alterarHabito(habito) {
    try {
      const resposta = await fetch(`${API}/${habito.id}`, {
        method: "PATCH",
        headers: {
          ...headersVisitante,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          concluido: !habito.concluido
        })
      });

      if (!resposta.ok) throw new Error();

      await carregarHabitos();
    } catch {
      setErro("Não foi possível atualizar o hábito.");
    }
  }

  async function excluirHabito(id) {
    try {
      const resposta = await fetch(`${API}/${id}`, {
        method: "DELETE",
        headers: headersVisitante
      });

      if (!resposta.ok) throw new Error();

      await carregarHabitos();
    } catch {
      setErro("Não foi possível excluir o hábito.");
    }
  }

  const concluidos = habitos.filter(function (habito) {
    return habito.concluido;
  }).length;

  const xpTotal = concluidos * 20;

  return (
    <main className="app">
      <header className="cabecalho">
        <div>
          <p className="subtitulo">PAINEL DE HÁBITOS</p>
          <h1>Daily<span>XP</span> Core</h1>
          <p className="descricao">
            Organize sua rotina. Conclua hábitos. Ganhe XP.
          </p>
        </div>
        <div className="selo">MODO FOCO</div>
      </header>

      <section className="estatisticas">
        <div className="stat">
          <span>XP TOTAL</span>
          <strong>{xpTotal}</strong>
        </div>

        <div className="stat">
          <span>HÁBITOS</span>
          <strong>{habitos.length}</strong>
        </div>

        <div className="stat">
          <span>CONCLUÍDOS</span>
          <strong>{concluidos}</strong>
        </div>
      </section>

      <section className="painel">
        <h2>Meus hábitos</h2>

        <form onSubmit={adicionarHabito} className="formulario">
          <label htmlFor="nomeHabito">Novo hábito</label>

          <div className="formulario-linha">
            <input
              id="nomeHabito"
              type="text"
              placeholder="Ex.: Estudar React"
              value={nome}
              onChange={(evento) => setNome(evento.target.value)}
              maxLength={100}
              required
            />
            <button type="submit" className="botao-principal">
              Adicionar
            </button>
          </div>
        </form>

        {erro && <p className="erro" role="alert">{erro}</p>}

        {carregando && <p>Carregando hábitos...</p>}

        {!carregando && habitos.length === 0 && (
          <p className="vazio">Nenhum hábito cadastrado ainda.</p>
        )}

        <div className="lista">
          {habitos.map(function (habito) {
            return (
              <article
                className={`habito ${habito.concluido ? "feito" : ""}`}
                key={habito.id}
              >
                <div className="habito-info">
                  <strong>{habito.nome}</strong>
                  <small>
                    {habito.concluido
                      ? "Concluído • +20 XP"
                      : "Pendente • 20 XP disponíveis"}
                  </small>
                </div>

                <div className="acoes">
                  <button
                    type="button"
                    className="botao-concluir"
                    onClick={() => alterarHabito(habito)}
                  >
                    {habito.concluido ? "Reabrir" : "Concluir"}
                  </button>

                  <button
                    type="button"
                    className="botao-excluir"
                    onClick={() => excluirHabito(habito.id)}
                    aria-label={`Excluir ${habito.nome}`}
                  >
                    Excluir
                  </button>
                </div>
              </article>
            );
          })}
        </div>
      </section>

      <footer>
        DailyXP Core • React + ASP.NET Core + PostgreSQL
      </footer>
    </main>
  );
}

export default App;
