def to_snake(name: str) -> str:
    """"PedidoItem" -> "pedido_item" """
    result = []

    for i, char in enumerate(name):
        if char.isupper():
            if i > 0:
                result.append("_")   # maiúscula no meio = começa palavra nova
            result.append(char.lower())
        else:
            result.append(char)

    return "".join(result)
