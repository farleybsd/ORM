package meuorm.models;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

// Campo calculado: é preenchido em consultas, mas nunca vai para INSERT/UPDATE
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.FIELD)
public @interface CalcField {
}
