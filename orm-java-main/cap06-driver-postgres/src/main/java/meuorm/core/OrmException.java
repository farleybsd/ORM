package meuorm.core;

public class OrmException extends RuntimeException {

    private static final long serialVersionUID = 1L;

    private final OrmErrorKind kind;
    private final String detail;   // nome da constraint ou da coluna, quando houver

    public OrmException(OrmErrorKind kind, String message) {
        this(kind, message, "", null);
    }

    public OrmException(OrmErrorKind kind, String message, String detail, Throwable cause) {
        super(message, cause);
        this.kind = kind;
        this.detail = detail == null ? "" : detail;
    }

    public OrmErrorKind getKind() {
        return kind;
    }

    public String getDetail() {
        return detail;
    }
}
