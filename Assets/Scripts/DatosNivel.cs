using System;
using System.IO;
using UnityEngine;

// Estructuras que reflejan exactamente nivel.json. La fuente de verdad del
// nivel es un solo archivo: si cambia alla, cambia aqui sin tocar nada mas.
[Serializable] public class Caja { public float[] p; public float[] s; }
[Serializable] public class Zona { public string id, nombre, funcion; public float x0, x1, z0, z1; }
[Serializable] public class Punto { public float x, z; }
[Serializable] public class InteractivoDato {
    public string id, tipo, zona, funcion, animacion, sonido;
    public float x, y, z, giro;
}
[Serializable] public class FugaDato { public float x, y, z; }
[Serializable] public class LuzDato { public float x, y, z, rango, intensidad; public string color; }
[Serializable] public class AguaDato { public float inicio, ritmo, por_fuga, maximo; }

[Serializable]
public class Nivel
{
    public string titulo;
    public float alto_muro;
    public Zona[] zonas_lista;
    public Caja[] pisos, muros, techos, plataformas;
    public InteractivoDato[] interactivos;
    public FugaDato[] fugas;
    public LuzDato[] luces;
    public AguaDato agua;
    public Punto entrada, salida;
    public Punto[] ruta;

    public static Nivel Cargar()
    {
        string ruta = Path.Combine(Application.dataPath, "nivel.json");
        return JsonUtility.FromJson<Nivel>(File.ReadAllText(ruta));
    }
}
