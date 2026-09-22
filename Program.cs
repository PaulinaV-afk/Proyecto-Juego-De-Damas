using System;
using System.Collections.Generic;

class JuegoDeDamas
{
    static char[,] tablero = new char[8, 8];
    static bool turnoJugador1 = true; // true = Jugador 1 ('X'), false = Jugador 2 ('O')
    
    // Lista para almacenar el historial de movimientos
    static List<RegistroMovimiento> historialMovimientos = new List<RegistroMovimiento>();
    static int contadorTurnos = 1;

    class RegistroMovimiento
    {
        public int NumeroTurno { get; }
        public string Jugador { get; }
        public int FilaOrigen { get; }
        public int ColumnaOrigen { get; }
        public int FilaDestino { get; }
        public int ColumnaDestino { get; }
        public string TipoAccion { get; }

        public RegistroMovimiento(int turno, string jugador, int fO, int cO, int fD, int cD, string tipo)
        {
            NumeroTurno = turno;
            Jugador = jugador;
            FilaOrigen = fO;
            ColumnaOrigen = cO;
            FilaDestino = fD;
            ColumnaDestino = cD;
            TipoAccion = tipo;
        }

        public override string ToString()
        {
            return $"Turno {NumeroTurno:D2} | {Jugador}: [{FilaOrigen},{ColumnaOrigen}] -> [{FilaDestino},{ColumnaDestino}] ({TipoAccion})";
        }
    }

    class OpcionMOV
    {
        public int Fila { get; }
        public int Columna { get; }
        public string Descripcion { get; }

        public OpcionMOV(int fila, int columna, string descripcion)
        {
            Fila = fila;
            Columna = columna;
            Descripcion = descripcion;
        }
    }

    static List<OpcionMOV> OBmovimientosDiponibles(int f, int c, bool soloCaptura, bool esJ1)
    {
        List<OpcionMOV> movimientosDisponibles = new List<OpcionMOV>();
        char pieza = tablero[f, c];
        bool esDama = (pieza == 'D' || pieza == 'K');
        List<Tuple<int, int, string>> direcciones = new List<Tuple<int, int, string>>();
        
        if (esDama)
        {
            direcciones.Add(new Tuple<int, int, string>(-1, -1, "Diagonal Arriba Izquierda"));
            direcciones.Add(new Tuple<int, int, string>(-1, 1, "Diagonal Arriba Derecha"));
            direcciones.Add(new Tuple<int, int, string>(1, -1, "Diagonal Abajo Izquierda"));
            direcciones.Add(new Tuple<int, int, string>(1, 1, "Diagonal Abajo Derecha"));
        }
        else
        {
            if (esJ1)
            {
                direcciones.Add(new Tuple<int, int, string>(-1, -1, "Izquierda"));
                direcciones.Add(new Tuple<int, int, string>(-1, 1, "Derecha"));
            }
            else
            {
                direcciones.Add(new Tuple<int, int, string>(1, -1, "Izquierda"));
                direcciones.Add(new Tuple<int, int, string>(1, 1, "Derecha"));
            }
        }

        foreach (var dir in direcciones)
        {
            int df = dir.Item1;
            int dc = dir.Item2;
            string nombre = dir.Item3;

            int fD2 = f + (df * 2);
            int cD2 = c + (dc * 2);
            int fInter = f + df;
            int cInter = c + dc;

            if (fD2 >= 0 && fD2 < 8 && cD2 >= 0 && cD2 < 8)
            {
                if (tablero[fD2, cD2] == '.' && EsEnemigo(pieza, tablero[fInter, cInter]))
                {
                    movimientosDisponibles.Add(new OpcionMOV(fD2, cD2, nombre + " (Captura)"));
                    continue;
                }
            }

            if (!soloCaptura)
            {
                int fD1 = f + df;
                int cD1 = c + dc;
                if (fD1 >= 0 && fD1 < 8 && cD1 >= 0 && cD1 < 8)
                {
                    if (tablero[fD1, cD1] == '.')
                    {
                        movimientosDisponibles.Add(new OpcionMOV(fD1, cD1, nombre));
                    }
                }
            }
        }

        return movimientosDisponibles;
    }

    static int PedirOpcionDireccion(int maxOpciones)
    {
        while (true)
        {
            Console.Write($"\nSelecciona una direccion (1-{maxOpciones}): ");
            string entrada = Console.ReadLine() ?? string.Empty;

            if (int.TryParse(entrada, out int seleccion) && seleccion >= 1 && seleccion <= maxOpciones)
            {
                return seleccion - 1;
            }

            Console.WriteLine($"Opcion invalida. Por favor ingresa un numero entre 1 y {maxOpciones}.");
        }
    }

    static void Main(string[] args)
    {
        Console.CursorVisible = true;

        MostrarReglasDelJuego();
        InicializarTablero();

        while (true)
        {
            Console.Clear();
            DibujarTablero();
            MostrarUltimosMovimientos(3); // Muestra los últimos 3 movimientos bajo el tablero

            if (VerificarFinDeJuego())
            {
                break;
            }

            string nombreJugadorActual = turnoJugador1 ? "Jugador 1 [X]" : "Jugador 2 [O]";
            Console.WriteLine($"\n--- Turno {contadorTurnos}: {nombreJugadorActual} ---");

            List<Tuple<int, int>> fichasConCaptura = ObtenerFichasConCapturaObligatoria(turnoJugador1);
            bool habiaCapturas = fichasConCaptura.Count > 0;

            int fOrigen, cOrigen;

            if (habiaCapturas)
            {
                Console.WriteLine("Atencion!, Es obligatorio comer. Selecciona una ficha con salto disponible.");
                PedirCoordenadas("Ingresa la Fila y Columna de la ficha a mover (ej. 5 2): ", out fOrigen, out cOrigen);

                bool esFichaValida = fichasConCaptura.Exists(p => p.Item1 == fOrigen && p.Item2 == cOrigen);
                if (!esFichaValida)
                {
                    MostrarMensaje("Error: Debes seleccionar una de las fichas que tienen captura obligatoria.");
                    continue;
                }
            }
            else
            {
                PedirCoordenadas("Ingresa Fila y Columna de la ficha a mover (ej. 5 2) o -1 para Salir: ", out fOrigen, out cOrigen);
                if (fOrigen == -1 || cOrigen == -1) return;
            }

            if (!EsFichaDelJugador(fOrigen, cOrigen, turnoJugador1))
            {
                MostrarMensaje("Error: La casilla seleccionada no contiene una de tus fichas.");
                continue;
            }

            List<OpcionMOV> opcionesDisponibles = OBmovimientosDiponibles(fOrigen, cOrigen, habiaCapturas, turnoJugador1);

            if (opcionesDisponibles.Count == 0)
            {
                MostrarMensaje("Error: La ficha seleccionada no tiene movimientos o saltos validos hacia ninguna direccion.");
                continue;
            }

            Console.WriteLine("\nDirecciones disponibles para mover esta ficha:");
            for (int i = 0; i < opcionesDisponibles.Count; i++)
            {
                Console.WriteLine($" [{i + 1}] {opcionesDisponibles[i].Descripcion} -> Casilla [{opcionesDisponibles[i].Fila}, {opcionesDisponibles[i].Columna}]");
            }

            int eleccionIndex = PedirOpcionDireccion(opcionesDisponibles.Count);
            OpcionMOV movElegido = opcionesDisponibles[eleccionIndex];

            int fDestino = movElegido.Fila;
            int cDestino = movElegido.Columna;

            bool seCapturo;
            if (IntentarMovimiento(fOrigen, cOrigen, fDestino, cDestino, habiaCapturas, turnoJugador1, out seCapturo))
            {
                bool corono = VerificarCoronacion(fDestino, cDestino);

                // Registrar en el historial
                string accion = seCapturo ? (corono ? "Captura y Coronacion" : "Captura") : (corono ? "Coronacion" : "Paso simple");
                historialMovimientos.Add(new RegistroMovimiento(contadorTurnos, nombreJugadorActual, fOrigen, cOrigen, fDestino, cDestino, accion));

                // Captura múltiple encadenada
                if (seCapturo)
                {
                    while (TieneCapturaDesde(fDestino, cDestino, turnoJugador1))
                    {
                        Console.Clear();
                        DibujarTablero();
                        MostrarUltimosMovimientos(3);
                        Console.WriteLine($"\nCaptura multiple disponible para la ficha en [{fDestino}, {cDestino}]!");

                        List<OpcionMOV> opcionesCapturaMult = OBmovimientosDiponibles(fDestino, cDestino, true, turnoJugador1);

                        Console.WriteLine("Direcciones disponibles para continuar comiendo:");
                        for (int i = 0; i < opcionesCapturaMult.Count; i++)
                        {
                            Console.WriteLine($" [{i + 1}] {opcionesCapturaMult[i].Descripcion} -> Casilla [{opcionesCapturaMult[i].Fila}, {opcionesCapturaMult[i].Columna}]");
                        }

                        int idxMult = PedirOpcionDireccion(opcionesCapturaMult.Count);
                        OpcionMOV movMult = opcionesCapturaMult[idxMult];

                        int fOrigenMult = fDestino;
                        int cOrigenMult = cDestino;

                        bool nuevaCaptura;
                        if (IntentarMovimiento(fDestino, cDestino, movMult.Fila, movMult.Columna, true, turnoJugador1, out nuevaCaptura) && nuevaCaptura)
                        {
                            fDestino = movMult.Fila;
                            cDestino = movMult.Columna;
                            bool coronoMult = VerificarCoronacion(fDestino, cDestino);

                            historialMovimientos.Add(new RegistroMovimiento(contadorTurnos, nombreJugadorActual, fOrigenMult, cOrigenMult, fDestino, cDestino, coronoMult ? "Captura Doble y Coronacion" : "Captura Doble"));
                        }
                        else
                        {
                            MostrarMensaje("Movimiento invalido. Debes realizar el salto de captura.");
                        }
                    }
                }

                // Siguiente turno
                contadorTurnos++;
                turnoJugador1 = !turnoJugador1;
            }
            else
            {
                MostrarMensaje("Movimiento invalido. Revisa las reglas de movimiento.");
            }
        }
    }

    static void MostrarUltimosMovimientos(int cantidad)
    {
        Console.WriteLine("\n--- ULTIMOS MOVIMIENTOS ---");
        if (historialMovimientos.Count == 0)
        {
            Console.WriteLine(" (Sin movimientos aun)");
            return;
        }

        int inicio = Math.Max(0, historialMovimientos.Count - cantidad);
        for (int i = inicio; i < historialMovimientos.Count; i++)
        {
            Console.WriteLine(" " + historialMovimientos[i].ToString());
        }
    }

    static void InicializarTablero()
    {
        for (int fila = 0; fila < 8; fila++)
        {
            for (int columna = 0; columna < 8; columna++)
            {
                if ((fila + columna) % 2 == 1)
                {
                    if (fila < 3)
                        tablero[fila, columna] = 'O';
                    else if (fila > 4)
                        tablero[fila, columna] = 'X';
                    else
                        tablero[fila, columna] = '.';
                }
                else
                {
                    tablero[fila, columna] = ' ';
                }
            }
        }
    }

    static void DibujarTablero()
    {
        Console.WriteLine("   0   1   2   3   4   5   6   7");
        Console.WriteLine(" ┌───┬───┬───┬───┬───┬───┬───┬───┐");

        for (int fila = 0; fila < 8; fila++)
        {
            Console.Write(fila + "│");
            for (int columna = 0; columna < 8; columna++)
            {
                char ficha = tablero[fila, columna];

                if ((fila + columna) % 2 == 1)
                {
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                }

                Console.Write($" {ficha} ");
                Console.ResetColor();
                Console.Write("│");
            }
            Console.WriteLine();

            if (fila < 7)
                Console.WriteLine(" ├───┼───┼───┼───┼───┼───┼───┼───┤");
        }
        Console.WriteLine(" └───┴───┴───┴───┴───┴───┴───┴───┘");
    }

    static void PedirCoordenadas(string mensaje, out int fila, out int columna)
    {
        fila = -1;
        columna = -1;
        while (true)
        {
            Console.Write(mensaje);
            string entrada = Console.ReadLine() ?? string.Empty;

            if (entrada == "-1") return;

            string[] partes = entrada.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 2 && int.TryParse(partes[0], out fila) && int.TryParse(partes[1], out columna))
            {
                if (fila >= 0 && fila < 8 && columna >= 0 && columna < 8)
                {
                    break;
                }
            }
            Console.WriteLine("Coordenadas invalidas. Ingresa dos numeros entre 0 y 7 separados por espacio.");
        }
    }

    static bool EsFichaDelJugador(int f, int c, bool esJ1)
    {
        char p = tablero[f, c];
        if (esJ1) return p == 'X' || p == 'D';
        return p == 'O' || p == 'K';
    }

    static bool EsEnemigo(char p1, char p2)
    {
        if (p1 == '.' || p1 == ' ' || p2 == '.' || p2 == ' ') return false;
        bool p1J1 = (p1 == 'X' || p1 == 'D');
        bool p2J1 = (p2 == 'X' || p2 == 'D');
        return p1J1 != p2J1;
    }

    static bool IntentarMovimiento(int fO, int cO, int fD, int cD, bool soloCaptura, bool esJ1, out bool seCapturo)
    {
        seCapturo = false;
        if (tablero[fD, cD] != '.') return false;

        char pieza = tablero[fO, cO];
        bool esDama = (pieza == 'D' || pieza == 'K');
        int deltaF = fD - fO;
        int deltaC = Math.Abs(cD - cO);

        if (!esDama)
        {
            if (esJ1 && deltaF >= 0) return false;
            if (!esJ1 && deltaF <= 0) return false;
        }

        if (Math.Abs(deltaF) == 1 && deltaC == 1)
        {
            if (soloCaptura) return false;
            tablero[fD, cD] = pieza;
            tablero[fO, cO] = '.';
            return true;
        }

        if (Math.Abs(deltaF) == 2 && deltaC == 2)
        {
            int fInter = (fO + fD) / 2;
            int cInter = (cO + cD) / 2;
            char piezaInter = tablero[fInter, cInter];

            if (EsEnemigo(pieza, piezaInter))
            {
                tablero[fD, cD] = pieza;
                tablero[fO, cO] = '.';
                tablero[fInter, cInter] = '.';
                seCapturo = true;
                return true;
            }
        }

        return false;
    }

    static List<Tuple<int, int>> ObtenerFichasConCapturaObligatoria(bool esJ1)
    {
        List<Tuple<int, int>> res = new List<Tuple<int, int>>();
        for (int f = 0; f < 8; f++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (EsFichaDelJugador(f, c, esJ1) && TieneCapturaDesde(f, c, esJ1))
                {
                    res.Add(new Tuple<int, int>(f, c));
                }
            }
        }
        return res;
    }

    static bool TieneCapturaDesde(int f, int c, bool esJ1)
    {
        char pieza = tablero[f, c];
        bool esDama = (pieza == 'D' || pieza == 'K');
        
        int[] df = esDama ? new int[] { -2, -2, 2, 2 } : (esJ1 ? new int[] { -2, -2 } : new int[] { 2, 2 });
        int[] dc = new int[] { -2, 2, -2, 2 };

        for (int i = 0; i < 4; i++)
        {
            int fD = f + df[i % df.Length];
            int cD = c + dc[i];
            int fInter = (f + fD) / 2;
            int cInter = (c + cD) / 2;

            if (fD >= 0 && fD < 8 && cD >= 0 && cD < 8)
            {
                if (tablero[fD, cD] == '.' && EsEnemigo(pieza, tablero[fInter, cInter]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    static bool VerificarCoronacion(int f, int c)
    {
        if (tablero[f, c] == 'X' && f == 0)
        {
            tablero[f, c] = 'D';
            return true;
        }
        if (tablero[f, c] == 'O' && f == 7)
        {
            tablero[f, c] = 'K';
            return true;
        }
        return false;
    }

    static bool VerificarFinDeJuego()
    {
        int fichasJ1 = 0, fichasJ2 = 0;
        bool movJ1 = false, movJ2 = false;

        for (int f = 0; f < 8; f++)
        {
            for (int c = 0; c < 8; c++)
            {
                char p = tablero[f, c];
                if (p == 'X' || p == 'D')
                {
                    fichasJ1++;
                    if (TieneCapturaDesde(f, c, true) || TieneMovimientoSimple(f, c, true)) movJ1 = true;
                }
                else if (p == 'O' || p == 'K')
                {
                    fichasJ2++;
                    if (TieneCapturaDesde(f, c, false) || TieneMovimientoSimple(f, c, false)) movJ2 = true;
                }
            }
        }

        if (fichasJ1 == 0 || !movJ1)
        {
            Console.WriteLine("\nJUEGO TERMINADO! Gana el Jugador 2 [O] (por eliminacion o acorralamiento).");
            return true;
        }
        if (fichasJ2 == 0 || !movJ2)
        {
            Console.WriteLine("\nJUEGO TERMINADO! Gana el Jugador 1 [X] (por eliminacion o acorralamiento).");
            return true;
        }

        return false;
    }

    static bool TieneMovimientoSimple(int f, int c, bool esJ1)
    {
        char pieza = tablero[f, c];
        bool esDama = (pieza == 'D' || pieza == 'K');
        int[] df = esDama ? new int[] { -1, -1, 1, 1 } : (esJ1 ? new int[] { -1, -1 } : new int[] { 1, 1 });
        int[] dc = new int[] { -1, 1, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int fD = f + df[i % df.Length];
            int cD = c + dc[i];
            if (fD >= 0 && fD < 8 && cD >= 0 && cD < 8 && tablero[fD, cD] == '.')
            {
                return true;
            }
        }
        return false;
    }

    static void MostrarMensaje(string msg)
    {
        Console.WriteLine($"\n--> {msg}");
        Console.WriteLine("Presiona cualquier tecla para continuar...");
        Console.ReadKey(true);
    }

    static void MostrarReglasDelJuego()
    {  
        while (true)
        {
            Console.Clear();
            Console.WriteLine("==========================================================================");
            Console.WriteLine("                PROYECTO ESTRUCTURA DE DATOS: JUEGO DE DAMAS              ");
            Console.WriteLine("==========================================================================");
            Console.WriteLine();
            Console.WriteLine("                          [1] INICIAR PARTIDA                             ");
            Console.WriteLine("                          [2] VER REGLAS DEL JUEGO                        ");
            Console.WriteLine("                          [3] SALIR                                       ");
            Console.WriteLine();
            Console.WriteLine("==========================================================================");
            Console.Write("Selecciona una opcion (1-3): ");

            string opcion = Console.ReadLine() ?? string.Empty;

            if (opcion == "1")
            {
                break;
            }
            else if (opcion == "2")
            {
                Console.Clear();
                Console.WriteLine("==========================================================================");
                Console.WriteLine("                            REGLAS DEL JUEGO                              ");
                Console.WriteLine("==========================================================================");
                Console.WriteLine(" 1. Tablero de 8x8.");
                Console.WriteLine(" 2. Juego de 2 jugadores:");
                Console.WriteLine("    - Jugador 1 (Fichas claras 'X' / Damas 'D') avanza hacia ARRIBA.");
                Console.WriteLine("    - Jugador 2 (Fichas oscuras 'O' / Damas 'K') avanza hacia ABAJO.");
                Console.WriteLine(" 3. Comienza el jugador con fichas claras (Jugador 1 [X]).");
                Console.WriteLine(" 4. Las fichas normales avanzan en diagonal y solo hacia adelante.");
                Console.WriteLine(" 5. Si tienes la posibilidad de comer, debes hacerlo.");
                Console.WriteLine(" 6. Si al comer puedes volver a comer, debes hacerlo.");
                Console.WriteLine(" 7. Al llegar al extremo opuesto, la ficha se convierte en Dama.");
                Console.WriteLine("    - Las Damas pueden moverse y comer hacia adelante y hacia atras en diagonal.");
                Console.WriteLine(" 8. Un jugador gana si destruye todas las fichas del oponente o");
                Console.WriteLine("    si lo deja sin movimientos posibles (acorralado).");
                Console.WriteLine("==========================================================================");
                Console.WriteLine("\nPresiona cualquier tecla para volver al menu principal...");
                Console.ReadKey(true);
            }
            else if (opcion == "3")
            {
                Environment.Exit(0);
            }
        }
    }
}