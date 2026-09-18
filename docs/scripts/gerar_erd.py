"""Gera o ERD do InvoiSys como SVG, a partir do schema real extraído do Postgres.

Escrito à mão em vez de usar Graphviz/Mermaid porque o ambiente não tem nenhum dos
dois e o resultado precisa ser versionável, legível em texto e estável entre gerações.
"""
import io

# ---------------------------------------------------------------- paleta
BG = "#0f1420"
GRID = "#1a2030"
CARD = "#161d2b"
CARD_HDR = "#1f2937"
BORDER = "#2b3648"
TXT = "#e5e9f0"
MUTED = "#8b98ad"
PK = "#f5b93c"
FK = "#5eb3f6"
UK = "#c084fc"
IDX = "#4ade80"
LINE = "#4a5costa"
LINE = "#46536b"
ACCENT = "#5eb3f6"
RESTRICT = "#fb7185"

FONT = "ui-monospace, 'Cascadia Code', 'JetBrains Mono', Consolas, monospace"
FONT_UI = "ui-sans-serif, 'Segoe UI', system-ui, sans-serif"

ROW_H = 19
HDR_H = 34
PAD = 10
CHAR_W = 6.55


def col(name, typ, kind=None, note=None):
    return {"name": name, "type": typ, "kind": kind, "note": note}


# ---------------------------------------------------------------- tabelas
TABLES = {
    "releases": {
        "x": 40, "y": 262, "sub": "agregado raiz",
        "cols": [
            col("id", "uuid", "pk"),
            col("chave_jira", "varchar(50)", "uk", "RELEASE-2026-08"),
            col("status", "varchar(30)", "idx", "StatusPipeline"),
            col("criado_em", "timestamptz"),
            col("atualizado_em", "timestamptz", None, "trigger"),
        ],
    },
    "historias_jira": {
        "x": 40, "y": 30, "sub": "dado bruto do Jira",
        "cols": [
            col("id", "uuid", "pk"),
            col("release_id", "uuid", "fk"),
            col("chave", "varchar(50)", "uk", "unique c/ release_id"),
            col("titulo", "text"),
            col("descricao_tecnica", "text"),
            col("tipo_issue", "varchar(50)"),
            col("texto_release_note", "text", None, "nullable"),
            col("labels", "text[]"),
            col("criado_em", "timestamptz"),
        ],
    },
    "execucoes_pipeline": {
        "x": 40, "y": 520, "sub": "rastreabilidade",
        "cols": [
            col("id", "uuid", "pk"),
            col("release_id", "uuid", "fk"),
            col("status", "varchar(30)", None, "StatusExecucaoPipeline"),
            col("modelo_llm", "varchar(100)", None, "nullable"),
            col("erro", "text", None, "nullable"),
            col("iniciado_em", "timestamptz"),
            col("concluido_em", "timestamptz", None, "nullable"),
        ],
    },
    "versoes_comunicado": {
        "x": 500, "y": 210, "sub": "1 por publico-alvo",
        "cols": [
            col("id", "uuid", "pk"),
            col("release_id", "uuid", "fk", "unique c/ publico"),
            col("publico", "varchar(30)", "uk", "PublicoAlvo"),
            col("status", "varchar(30)", "idx", "StatusRevisao"),
            col("titulo_executivo", "text", None, "nullable"),
            col("resumo_executivo", "text", None, "nullable"),
            col("revisado_por", "varchar(200)", None, "nullable"),
            col("revisado_em", "timestamptz", None, "nullable"),
            col("motivo_reprovacao", "text", None, "CHECK se reprovado"),
            col("criado_em", "timestamptz"),
            col("atualizado_em", "timestamptz", None, "trigger"),
        ],
    },
    "itens_comunicado": {
        "x": 500, "y": 545, "sub": "paragrafo do comunicado",
        "cols": [
            col("id", "uuid", "pk"),
            col("versao_comunicado_id", "uuid", "fk"),
            col("categoria", "varchar(30)", None, "CategoriaAlteracao"),
            col("texto", "text", None, "gerado pela IA"),
            col("texto_editado_manualmente", "text", None, "nullable"),
            col("origens", "text[]", "idx", "GIN - chaves Jira"),
            col("incluido", "boolean", None, "default true"),
            col("motivo_exclusao", "text", None, "nullable"),
            col("criado_em", "timestamptz"),
            col("atualizado_em", "timestamptz", None, "trigger"),
        ],
    },
    "comunicados_exportados": {
        "x": 962, "y": 262, "sub": "historico de publicacao",
        "cols": [
            col("id", "uuid", "pk"),
            col("versao_comunicado_id", "uuid", "fk", "RESTRICT"),
            col("release_id", "uuid", "fk", "CASCADE"),
            col("publico", "varchar(30)", None, "espelha a versao"),
            col("formato", "varchar(20)", None, "FormatoExportacao"),
            col("conteudo", "text", None, "MD / HTML"),
            col("caminho_arquivo", "text", None, "PDF"),
            col("gerado_por", "varchar(200)", None, "nullable"),
            col("gerado_em", "timestamptz"),
        ],
    },
    "usuarios": {
        "x": 962, "y": 30, "sub": "auth (sem FK ainda)", "detached": True,
        "cols": [
            col("id", "uuid", "pk"),
            col("nome", "varchar(200)"),
            col("email", "varchar(320)", "uk"),
            col("senha_hash", "varchar(255)"),
            col("papel", "varchar(50)", None, "texto livre"),
            col("avatar_url", "text", None, "nullable"),
            col("ativo", "boolean", None, "default true"),
            col("criado_em", "timestamptz"),
        ],
    },
    "destaques_hero": {
        "x": 962, "y": 545, "sub": "conteudo da tela de login", "detached": True,
        "cols": [
            col("id", "uuid", "pk"),
            col("titulo", "varchar(200)"),
            col("descricao", "text"),
            col("icone", "varchar(50)"),
            col("ordem", "integer", "idx", "idx c/ ativo"),
            col("ativo", "boolean", None, "default true"),
            col("criado_em", "timestamptz"),
        ],
    },
}


def width_of(t):
    w = 0
    for c in t["cols"]:
        s = len(c["name"]) + len(c["type"]) + 3
        if c["note"]:
            s += len(c["note"]) + 3
        w = max(w, s)
    return max(268, int(w * CHAR_W) + PAD * 2 + 16)


for t in TABLES.values():
    t["w"] = width_of(t)
    t["h"] = HDR_H + len(t["cols"]) * ROW_H + PAD


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def row_y(t, i):
    return t["y"] + HDR_H + i * ROW_H + ROW_H / 2 + 0.5


def idx_of(tname, colname):
    for i, c in enumerate(TABLES[tname]["cols"]):
        if c["name"] == colname:
            return i
    raise KeyError(colname)


# ---------------------------------------------------------------- relacionamentos
# (origem, col_origem, destino, col_destino, cardinalidade, on_delete, lado)
RELS = [
    # (origem, col_origem, destino, col_destino, on_delete, lado, canal_x)
    ("historias_jira", "release_id", "releases", "id", "CASCADE", "left", 440),
    ("execucoes_pipeline", "release_id", "releases", "id", "CASCADE", "left", 415),
    ("versoes_comunicado", "release_id", "releases", "id", "CASCADE", "gap", 466),
    ("itens_comunicado", "versao_comunicado_id", "versoes_comunicado", "id", "CASCADE", "vert", 0),
    ("comunicados_exportados", "versao_comunicado_id", "versoes_comunicado", "id", "RESTRICT", "gap2", 1286),
    ("comunicados_exportados", "release_id", "releases", "id", "CASCADE", "gap2", 1330),
]

W, H = 1400, 940
out = []
a = out.append

a(f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" '
  f'viewBox="0 0 {W} {H}" font-family="{FONT}">')
a('<defs>')
a(f'<pattern id="grid" width="28" height="28" patternUnits="userSpaceOnUse">'
  f'<path d="M28 0H0V28" fill="none" stroke="{GRID}" stroke-width="1"/></pattern>')
a(f'<marker id="crow" viewBox="0 0 12 12" refX="11" refY="6" markerWidth="11" '
  f'markerHeight="11" orient="auto-start-reverse">'
  f'<path d="M12 6H2M12 6 2 1M12 6 2 11" fill="none" stroke="{LINE}" stroke-width="1.5"/></marker>')
a(f'<marker id="crowR" viewBox="0 0 12 12" refX="11" refY="6" markerWidth="11" '
  f'markerHeight="11" orient="auto-start-reverse">'
  f'<path d="M12 6H2M12 6 2 1M12 6 2 11" fill="none" stroke="{RESTRICT}" stroke-width="1.5"/></marker>')
a(f'<marker id="one" viewBox="0 0 12 12" refX="1" refY="6" markerWidth="11" '
  f'markerHeight="11" orient="auto-start-reverse">'
  f'<path d="M4 1V11" fill="none" stroke="{LINE}" stroke-width="1.8"/></marker>')
a(f'<marker id="oneR" viewBox="0 0 12 12" refX="1" refY="6" markerWidth="11" '
  f'markerHeight="11" orient="auto-start-reverse">'
  f'<path d="M4 1V11" fill="none" stroke="{RESTRICT}" stroke-width="1.8"/></marker>')
a('<filter id="sh" x="-20%" y="-20%" width="150%" height="150%">'
  '<feDropShadow dx="0" dy="3" stdDeviation="5" flood-color="#000" flood-opacity="0.45"/></filter>')
a('</defs>')

a(f'<rect width="{W}" height="{H}" fill="{BG}"/>')
a(f'<rect width="{W}" height="{H}" fill="url(#grid)"/>')

# título
a(f'<text x="40" y="922" font-family="{FONT_UI}" font-size="12" fill="{MUTED}">'
  f'InvoiSys — modelo relacional (PostgreSQL 16). Gerado do schema real aplicado pela migration.</text>')

# --------------------------------------------------- linhas de relacionamento
# Cada relacionamento tem um corredor vertical proprio (canal_x), escolhido para nao
# cruzar nenhum card. O label fica sobre o trecho vertical, onde sempre ha espaco.
for src, scol, dst, dcol, ondel, side, chan in RELS:
    s_, d_ = TABLES[src], TABLES[dst]
    y1 = row_y(s_, idx_of(src, scol))
    y2 = row_y(d_, idx_of(dst, dcol))
    restrict = ondel == "RESTRICT"
    stroke = RESTRICT if restrict else LINE
    mk_many = "crowR" if restrict else "crow"
    mk_one = "oneR" if restrict else "one"
    dash = ' stroke-dasharray="6 4"' if restrict else ""
    R = 12

    if side == "vert":
        # Cards alinhados verticalmente: liga topo do filho ao rodape do pai, reto.
        cx = s_["x"] + s_["w"] * 0.32
        ytop = s_["y"]
        ybot = d_["y"] + d_["h"]
        a(f'<path d="M{cx} {ytop} V{ybot}" fill="none" stroke="{stroke}" '
          f'stroke-width="1.6" marker-start="url(#{mk_many})" marker-end="url(#{mk_one})"/>')
        label = f'1:N {ondel}'
        ly_ = (ytop + ybot) / 2
        tw = len(label) * 5.5 + 12
        a(f'<g transform="translate({cx},{ly_}) rotate(-90)">')
        a(f'<rect x="{-tw / 2}" y="-8" width="{tw}" height="16" rx="4" fill="{BG}" '
          f'stroke="{stroke}" stroke-width="0.8"/>')
        a(f'<text x="0" y="3.5" font-size="9" fill="{stroke}" text-anchor="middle">{label}</text>')
        a('</g>')
        continue

    # lado de saida/chegada de cada card em funcao do canal
    x1 = s_["x"] + s_["w"] if chan > s_["x"] + s_["w"] / 2 else s_["x"]
    x2 = d_["x"] + d_["w"] if chan > d_["x"] + d_["w"] / 2 else d_["x"]

    s1 = 1 if chan > x1 else -1          # direcao horizontal na saida
    s2 = 1 if chan > x2 else -1          # direcao horizontal na chegada
    vy = 1 if y2 > y1 else -1            # direcao vertical no corredor

    path = (f'M{x1} {y1} H{chan - s1 * R} Q{chan} {y1} {chan} {y1 + vy * R} '
            f'V{y2 - vy * R} Q{chan} {y2} {chan - s2 * R} {y2} H{x2}')

    a(f'<path d="{path}" fill="none" stroke="{stroke}" stroke-width="1.6"{dash} '
      f'marker-start="url(#{mk_many})" marker-end="url(#{mk_one})"/>')

    # label sobre o corredor vertical, no ponto medio
    label = f'1:N {ondel}'
    ly_ = (y1 + y2) / 2
    tw = len(label) * 5.5 + 12
    a(f'<g transform="translate({chan},{ly_}) rotate(-90)">')
    a(f'<rect x="{-tw / 2}" y="-8" width="{tw}" height="16" rx="4" fill="{BG}" '
      f'stroke="{stroke}" stroke-width="0.8"/>')
    a(f'<text x="0" y="3.5" font-size="9" fill="{stroke}" text-anchor="middle">{label}</text>')
    a('</g>')

# --------------------------------------------------- tabelas
for name, t in TABLES.items():
    x, y, w, h = t["x"], t["y"], t["w"], t["h"]
    a(f'<g filter="url(#sh)">')
    a(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="9" fill="{CARD}" '
      f'stroke="{BORDER}" stroke-width="1.2"/>')
    a('</g>')
    a(f'<path d="M{x} {y + HDR_H} h{w}" stroke="{BORDER}" stroke-width="1.2"/>')
    a(f'<path d="M{x + 9} {y} h{w - 18} a9 9 0 0 1 9 9 v{HDR_H - 9} h-{w} v-{HDR_H - 9} '
      f'a9 9 0 0 1 9 -9 z" fill="{CARD_HDR}"/>')

    dashed = t.get("detached")
    if dashed:
        a(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="9" fill="none" '
          f'stroke="{MUTED}" stroke-width="1.2" stroke-dasharray="5 4" opacity="0.5"/>')

    a(f'<text x="{x + PAD}" y="{y + 16}" font-size="12.5" font-weight="700" fill="{TXT}">'
      f'{esc(name)}</text>')
    a(f'<text x="{x + PAD}" y="{y + 28}" font-size="9" fill="{MUTED}" '
      f'font-family="{FONT_UI}">{esc(t["sub"])}</text>')

    for i, c in enumerate(t["cols"]):
        cy = y + HDR_H + i * ROW_H + 13
        if i % 2 == 1:
            a(f'<rect x="{x + 1}" y="{y + HDR_H + i * ROW_H}" width="{w - 2}" '
              f'height="{ROW_H}" fill="#ffffff" fill-opacity="0.018"/>')

        kind = c["kind"]
        badge = {"pk": ("PK", PK), "fk": ("FK", FK), "uk": ("UK", UK), "idx": ("IX", IDX)}.get(kind)
        tx = x + PAD
        if badge:
            lbl, color = badge
            a(f'<rect x="{tx}" y="{cy - 9}" width="19" height="12.5" rx="3" '
              f'fill="{color}" fill-opacity="0.16" stroke="{color}" stroke-width="0.7"/>')
            a(f'<text x="{tx + 9.5}" y="{cy + 0.5}" font-size="7.8" font-weight="700" '
              f'fill="{color}" text-anchor="middle">{lbl}</text>')
        tx += 24

        weight = "600" if kind in ("pk", "fk", "uk") else "400"
        fill = TXT if kind else "#c9d1e0"
        a(f'<text x="{tx}" y="{cy}" font-size="10.5" font-weight="{weight}" fill="{fill}">'
          f'{esc(c["name"])}</text>')
        tx += len(c["name"]) * CHAR_W + 8
        a(f'<text x="{tx}" y="{cy}" font-size="9.5" fill="{MUTED}">{esc(c["type"])}</text>')
        if c["note"]:
            tx += len(c["type"]) * 5.9 + 9
            note_color = RESTRICT if c["note"] == "RESTRICT" else (
                IDX if "GIN" in c["note"] or "idx" in c["note"] else "#6e7e96")
            a(f'<text x="{tx}" y="{cy}" font-size="8.6" fill="{note_color}" '
              f'font-style="italic">{esc(c["note"])}</text>')

# --------------------------------------------------- legenda
lx, ly = 40, 800
a(f'<rect x="{lx}" y="{ly}" width="1320" height="92" rx="9" fill="{CARD}" '
  f'stroke="{BORDER}" stroke-width="1.2"/>')
a(f'<text x="{lx + 16}" y="{ly + 21}" font-size="10.5" font-weight="700" fill="{TXT}">LEGENDA</text>')

items = [
    (PK, "PK", "chave primaria (uuid, gen_random_uuid())"),
    (FK, "FK", "chave estrangeira"),
    (UK, "UK", "restricao unique"),
    (IDX, "IX", "indice nao-unico"),
]
for i, (color, lbl, desc) in enumerate(items):
    ix = lx + 16 + (i % 2) * 300
    iy = ly + 42 + (i // 2) * 21
    a(f'<rect x="{ix}" y="{iy - 9}" width="19" height="12.5" rx="3" fill="{color}" '
      f'fill-opacity="0.16" stroke="{color}" stroke-width="0.7"/>')
    a(f'<text x="{ix + 9.5}" y="{iy + 0.5}" font-size="7.8" font-weight="700" '
      f'fill="{color}" text-anchor="middle">{lbl}</text>')
    a(f'<text x="{ix + 27}" y="{iy}" font-size="9.5" fill="{MUTED}">{esc(desc)}</text>')

a(f'<path d="M{lx + 640} {ly + 36} h34" stroke="{LINE}" stroke-width="1.6" '
  f'marker-start="url(#crow)" marker-end="url(#one)"/>')
a(f'<text x="{lx + 686} " y="{ly + 39}" font-size="9.5" fill="{MUTED}">'
  f'1:N — ON DELETE CASCADE</text>')
a(f'<path d="M{lx + 640} {ly + 60} h34" stroke="{RESTRICT}" stroke-width="1.6" '
  f'stroke-dasharray="6 4" marker-start="url(#crowR)" marker-end="url(#oneR)"/>')
a(f'<text x="{lx + 686}" y="{ly + 63}" font-size="9.5" fill="{RESTRICT}">'
  f'1:N — ON DELETE RESTRICT (protege auditoria)</text>')
a(f'<rect x="{lx + 640}" y="{ly + 72}" width="20" height="11" rx="3" fill="none" '
  f'stroke="{MUTED}" stroke-width="1.1" stroke-dasharray="4 3" opacity="0.7"/>')
a(f'<text x="{lx + 686}" y="{ly + 81}" font-size="9.5" fill="{MUTED}">'
  f'borda tracejada — tabela sem FK (ver pendencias)</text>')

a('</svg>')

io.open("C:/DEV/squad78-invoisys/docs/erd.svg", "w", encoding="utf-8").write("\n".join(out))
print("ERD gerado:", sum(len(t["cols"]) for t in TABLES.values()), "colunas,",
      len(TABLES), "tabelas,", len(RELS), "relacionamentos")
