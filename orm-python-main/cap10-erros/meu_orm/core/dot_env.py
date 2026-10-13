import os
from pathlib import Path


def load(path: str = ".env") -> None:
    """Lê o arquivo e coloca cada CHAVE=valor nas variáveis de ambiente do processo."""
    arquivo = Path(path)
    if not arquivo.exists():
        return  # em produção as variáveis podem vir do próprio servidor

    for linha in arquivo.read_text(encoding="utf-8").splitlines():
        texto = linha.strip()

        if texto == "" or texto.startswith("#"):
            continue  # linha vazia ou comentário

        chave, separador, valor = texto.partition("=")
        if not separador or not chave.strip():
            continue  # linha sem "=" é ignorada

        os.environ[chave.strip()] = valor.strip()


def get(chave: str) -> str:
    """Busca uma variável obrigatória e explica o erro se ela não existir."""
    valor = os.environ.get(chave)
    if valor is None:
        raise RuntimeError(f"A variável '{chave}' não foi encontrada. Confira o arquivo .env")
    return valor
