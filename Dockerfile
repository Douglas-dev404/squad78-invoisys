# syntax=docker/dockerfile:1
#
# Multi-stage build: estágio de build instala dependências num venv isolado,
# estágio final copia só o venv + código — nada de cache de pip, headers de
# compilação, ou uv sobrando na imagem que vai rodar.
#
# Por padrão instala só as dependências principais (sem LangChain, que fica no
# grupo opcional [llm] — só entra quando o adapter real do LLM for implementado,
# evitando pesar a imagem com uma lib que ainda não está em uso).

FROM python:3.12-slim AS builder

WORKDIR /app

RUN pip install --no-cache-dir uv

COPY pyproject.toml ./
COPY app ./app

# --no-cache mantém a camada de build enxuta; troque para "-e .[llm]" se algum dia
# o build padrão precisar já vir com LangChain instalado.
RUN uv venv /opt/venv \
    && . /opt/venv/bin/activate \
    && uv pip install --no-cache -e .

FROM python:3.12-slim AS runtime

RUN groupadd --system app && useradd --system --gid app app

WORKDIR /app

COPY --from=builder /opt/venv /opt/venv
COPY app ./app
COPY prompts ./prompts

ENV PATH="/opt/venv/bin:$PATH" \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

USER app

EXPOSE 8000

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/health')" || exit 1

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
