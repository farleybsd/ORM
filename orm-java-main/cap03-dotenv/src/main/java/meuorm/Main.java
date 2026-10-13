package meuorm;

import meuorm.core.DotEnv;

public class Main {

    public static void main(String[] args) {
        DotEnv.load();
        System.out.println("Driver: " + DotEnv.get("ORM_DRIVER"));
        System.out.println("Banco: " + DotEnv.get("DB_NAME"));
    }
}
