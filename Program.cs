using System;
using System.Collections.Generic;

class JuegoDeDamas
{
    static char[,] tablero = new char[8, 8];
    static bool turnoJugador1 = true; // true = Jugador 1 ('X'), false = Jugador 2 ('O')

    static void Main(string[] args)
    {

        Console.CursorVisible = true;

        MostrarReglasDelJuego();

        InicializarTablero();

        while (true)
        {
            Console.Clear();
            DibujarTablero();

            // Verificar fin de juego al inicio del turno
            if (VerificarFinDeJuego())
            {
                break;
            }

            Console.WriteLine($"\n--- Turno del Jugador {(turnoJugador1 ? "1 [X / Dama: D]" : "2 [O / Dama: K]")} ---");

            // Validar si hay capturas obligatorias para el jugador actual
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

            // Verificar que la casilla contenga una ficha propia
            if (!EsFichaDelJugador(fOrigen, cOrigen, turnoJugador1))
            {
                MostrarMensaje("Error: La casilla seleccionada no contiene una de tus fichas.");
                continue;
            }

            // Pedir casilla destino
            int fDestino, cDestino;
            PedirCoordenadas("Ingresa Fila y Columna de destino (ej. 4 3): ", out fDestino, out cDestino);

            // Intentar realizar el movimiento
            bool seCapturo;
            if (IntentarMovimiento(fOrigen, cOrigen, fDestino, cDestino, habiaCapturas, turnoJugador1, out seCapturo))
            {
                VerificarCoronacion(fDestino, cDestino);

                // Captura multiple encadenada
                if (seCapturo)
                {
                    while (TieneCapturaDesde(fDestino, cDestino, turnoJugador1))
                    {
                        Console.Clear();
                        DibujarTablero();
                        Console.WriteLine($"\nCaptura multiple disponible para la ficha en [{fDestino}, {cDestino}]!");

                        int fSigDest, cSigDest;
                        PedirCoordenadas("Ingresa el siguiente destino para continuar comiendo: ", out fSigDest, out cSigDest);

                        bool nuevaCaptura;
                        if (IntentarMovimiento(fDestino, cDestino, fSigDest, cSigDest, true, turnoJugador1, out nuevaCaptura) && nuevaCaptura)
                        {
                            fDestino = fSigDest;
                            cDestino = cSigDest;
                            VerificarCoronacion(fDestino, cDestino);
                        }
                        else
                        {
                            MostrarMensaje("Movimiento invalido. Debes realizar el salto de captura.");
                        }
                    }
                }

                // Cambiar el turno
                turnoJugador1 = !turnoJugador1;
            }
            else
            {
                MostrarMensaje("Movimiento invalido. Revisa las reglas de movimiento.");
            }
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
                        tablero[fila, columna] = 'O'; // Jugador 2 (avanza hacia abajo, fila mayores)
                    else if (fila > 4)
                        tablero[fila, columna] = 'X'; // Jugador 1 (avanza hacia arriba, fila menores)
                    else
                        tablero[fila, columna] = '.'; // Casilla vacia jugable
                }
                else
                {
                    tablero[fila, columna] = ' '; // Casilla no jugable
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

        // Validar direccion para fichas normales
        if (!esDama)
        {
            if (esJ1 && deltaF >= 0) return false;  // Jugador 1 'X' solo avanza hacia arriba (filas menores, deltaF < 0)
            if (!esJ1 && deltaF <= 0) return false; // Jugador 2 'O' solo avanza hacia abajo (filas mayores, deltaF > 0)
        }

        // Movimiento simple (1 paso en diagonal)
        if (Math.Abs(deltaF) == 1 && deltaC == 1)
        {
            if (soloCaptura) return false;
            tablero[fD, cD] = pieza;
            tablero[fO, cO] = '.';
            return true;
        }

        // Movimiento de captura (2 pasos en diagonal)
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

    static void VerificarCoronacion(int f, int c)
    {
        if (tablero[f, c] == 'X' && f == 0) tablero[f, c] = 'D'; // Dama Jugador 1
        if (tablero[f, c] == 'O' && f == 7) tablero[f, c] = 'K'; // Dama Jugador 2
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
            break; // Sale del menu de inicio e inicia la partida
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
            Console.WriteLine(" 6. Si al comer puedes volver a comer, debes hacerlo .");
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
            Environment.Exit(0); // Se cierra el programa
        }
    }
}
}