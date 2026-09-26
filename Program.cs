using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

class JuegoDeDamas
{
    // Representacion del tablero de 8x8 casillas
    static char[,] tablero = new char[8, 8];
    
    // Control de turnos: true indica el turno del jugador 1 ('X'), false el del jugador 2 ('O')
    static bool turnoJugador1 = true;

    // Lista para guardar cada movimiento hecho en la partida actual
    static List<RegistroMovimiento> historialMovimientos = new List<RegistroMovimiento>();
    
    // Contador global de turnos de la partida
    static int contadorTurnos = 1;
    
    // Reloj global por jugador en segundos (120 segundos = 2 minutos)
    static double tiempoRestanteJ1 = 120.0;
    static double tiempoRestanteJ2 = 120.0;

    // Limite de tiempo por turno
    static int tiempoLimiteSegundos = 30;

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

    // Limpia la consola completamente usando Console.Clear
    static void LimpiarPantalla()
    {
        Console.Clear();
    }

    static void Main(string[] args)
    {
        Console.CursorVisible = false;

        while (true)
        {
            MostrarReglasDelJuego();

            int cursorFila = 5;
            int cursorColumna = 0;
            bool salirAlMenu = false;

            while (!salirAlMenu)
            {
                if (VerificarFinDeJuego())
                {
                    break;
                }

                string nombreJugadorActual = turnoJugador1 ? "Jugador 1 [X]" : "Jugador 2 [O]";

                List<Tuple<int, int>> fichasConCaptura = ObtenerFichasConCapturaObligatoria(turnoJugador1);
                bool habiaCapturas = fichasConCaptura.Count > 0;

                DateTime tiempoInicioTurno = DateTime.Now;

                int fOrigen = -1, cOrigen = -1;
                bool fichaSeleccionada = false;
                int ultimoSegundoMostrado = -1;
                string mensajeAlertaActual = "";
                bool necesitaRedibujar = true;

                // Bucle de Seleccion de Ficha
                while (!fichaSeleccionada && !salirAlMenu)
                {
                    DateTime ahora = DateTime.Now;
                    double delta = (ahora - tiempoInicioTurno).TotalSeconds;
                    tiempoInicioTurno = ahora;

                    if (turnoJugador1)
                    {
                        tiempoRestanteJ1 -= delta;
                        if (tiempoRestanteJ1 <= 0)
                        {
                            tiempoRestanteJ1 = 0;
                            break;
                        }
                    }
                    else
                    {
                        tiempoRestanteJ2 -= delta;
                        if (tiempoRestanteJ2 <= 0)
                        {
                            tiempoRestanteJ2 = 0;
                            break;
                        }
                    }

                    double tiempoActualJugador = turnoJugador1 ? tiempoRestanteJ1 : tiempoRestanteJ2;

                    if (tiempoActualJugador <= 0)
                    {
                        break;
                    }

                    if (necesitaRedibujar || (int)tiempoActualJugador != ultimoSegundoMostrado)
                    {
                        ultimoSegundoMostrado = (int)tiempoActualJugador;
                        necesitaRedibujar = false;

                        LimpiarPantalla();
                        DibujarTablero(cursorFila, cursorColumna);
                        MostrarUltimosMovimientos(2);

                        Console.WriteLine($"\n--- Turno {contadorTurnos}: {(turnoJugador1 ? "Jugador 1 [X]" : "Jugador 2 [O]")} ---");
                        Console.WriteLine($"Tiempo Reloj J1: {(int)tiempoRestanteJ1}s | Reloj J2: {(int)tiempoRestanteJ2}s");
                        Console.WriteLine("FLECHAS: Mover | [ENTER]: Seleccionar | [S]: Guardar | [Q]: Salir");

                        if (habiaCapturas)
                        {
                            Console.WriteLine("Atencion: Es obligatorio comer. Selecciona una ficha con salto disponible.");
                        }

                        if (!string.IsNullOrEmpty(mensajeAlertaActual))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"--> {mensajeAlertaActual}");
                            Console.ResetColor();
                        }
                    }

                    if (Console.KeyAvailable)
                    {
                        ConsoleKey key = Console.ReadKey(true).Key;
                        mensajeAlertaActual = "";
                        necesitaRedibujar = true;

                        MoverCursor(key, ref cursorFila, ref cursorColumna);

                        if (key == ConsoleKey.S)
                        {
                            Console.Clear();
                            MenuGuardarPartida();
                        }

                        if (key == ConsoleKey.Q)
                        {
                            Console.Clear();
                            Console.WriteLine("¿Deseas salir al menu principal? (S/N)");
                            ConsoleKey resp = Console.ReadKey(true).Key;
                            if (resp == ConsoleKey.S)
                            {
                                salirAlMenu = true;
                                break;
                            }
                        }

                        if (key == ConsoleKey.Enter)
                        {
                            if (EsFichaDelJugador(cursorFila, cursorColumna, turnoJugador1))
                            {
                                if (habiaCapturas && !fichasConCaptura.Exists(p => p.Item1 == cursorFila && p.Item2 == cursorColumna))
                                {
                                    mensajeAlertaActual = "Error: Debes seleccionar una ficha con captura obligatoria.";
                                }
                                else
                                {
                                    List<OpcionMOV> ops = OBmovimientosDiponibles(cursorFila, cursorColumna, habiaCapturas, turnoJugador1);
                                    if (ops.Count == 0)
                                    {
                                        mensajeAlertaActual = "Error: La ficha seleccionada no tiene movimientos validos.";
                                    }
                                    else
                                    {
                                        fOrigen = cursorFila;
                                        cOrigen = cursorColumna;
                                        fichaSeleccionada = true;
                                    }
                                }
                            }
                            else
                            {
                                mensajeAlertaActual = "Error: La casilla seleccionada no contiene una de tus fichas.";
                            }
                        }
                    }

                    Thread.Sleep(50);
                }

                if (salirAlMenu) break;

                // Si el tiempo del jugador se agoto, fuerza el reinicio del ciclo principal para ejecutar VerificarFinDeJuego
                if (tiempoRestanteJ1 <= 0 || tiempoRestanteJ2 <= 0)
                {
                    continue;
                }

                // Seleccion de Destino
                List<OpcionMOV> opcionesDisponibles = OBmovimientosDiponibles(fOrigen, cOrigen, habiaCapturas, turnoJugador1);
                int opcionIndex = 0;
                bool direccionConfirmada = false;
                bool cancelarSeleccion = false;
                necesitaRedibujar = true;

                while (!direccionConfirmada && !cancelarSeleccion && !salirAlMenu)
                {
                    DateTime ahora = DateTime.Now;
                    double delta = (ahora - tiempoInicioTurno).TotalSeconds;
                    tiempoInicioTurno = ahora;

                    if (turnoJugador1)
                    {
                        tiempoRestanteJ1 -= delta;
                        if (tiempoRestanteJ1 <= 0) break;
                    }
                    else
                    {
                        tiempoRestanteJ2 -= delta;
                        if (tiempoRestanteJ2 <= 0) break;
                    }

                    OpcionMOV movActual = opcionesDisponibles[opcionIndex];

                    if (necesitaRedibujar)
                    {
                        necesitaRedibujar = false;
                        LimpiarPantalla();
                        DibujarTablero(movActual.Fila, movActual.Columna, fOrigen, cOrigen);
                        MostrarUltimosMovimientos(2);

                        Console.WriteLine("\nDirecciones disponibles para mover esta ficha:");
                        Console.WriteLine("Usa FLECHAS (Arriba/Abajo) y ENTER para confirmar (ESC para cancelar):\n");

                        for (int i = 0; i < opcionesDisponibles.Count; i++)
                        {
                            if (i == opcionIndex)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($" > [{i + 1}] {opcionesDisponibles[i].Descripcion} -> Casilla [{opcionesDisponibles[i].Fila}, {opcionesDisponibles[i].Columna}]");
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.WriteLine($"   [{i + 1}] {opcionesDisponibles[i].Descripcion} -> Casilla [{opcionesDisponibles[i].Fila}, {opcionesDisponibles[i].Columna}]");
                            }
                        }
                    }

                    if (Console.KeyAvailable)
                    {
                        ConsoleKey key = Console.ReadKey(true).Key;
                        necesitaRedibujar = true;

                        if (key == ConsoleKey.UpArrow)
                        {
                            opcionIndex = (opcionIndex - 1 + opcionesDisponibles.Count) % opcionesDisponibles.Count;
                        }
                        else if (key == ConsoleKey.DownArrow)
                        {
                            opcionIndex = (opcionIndex + 1) % opcionesDisponibles.Count;
                        }
                        else if (key == ConsoleKey.Enter)
                        {
                            direccionConfirmada = true;
                        }
                        else if (key == ConsoleKey.Escape)
                        {
                            cancelarSeleccion = true;
                        }
                    }

                    Thread.Sleep(50);
                }

                // Si se agoto el tiempo durante la seleccion de destino, reinicia el ciclo principal
                if (tiempoRestanteJ1 <= 0 || tiempoRestanteJ2 <= 0)
                {
                    continue;
                }

                if (cancelarSeleccion)
                {
                    cursorFila = fOrigen;
                    cursorColumna = cOrigen;
                    Console.Clear();
                    continue;
                }

                if (direccionConfirmada)
                {
                    OpcionMOV movElegido = opcionesDisponibles[opcionIndex];
                    int fDestino = movElegido.Fila;
                    int cDestino = movElegido.Columna;

                    bool seCapturo;
                    if (IntentarMovimiento(fOrigen, cOrigen, fDestino, cDestino, habiaCapturas, turnoJugador1, out seCapturo))
                    {
                        bool corono = VerificarCoronacion(fDestino, cDestino);
                        string accion = seCapturo ? (corono ? "Captura y Coronacion" : "Captura") : (corono ? "Coronacion" : "Paso simple");
                        historialMovimientos.Add(new RegistroMovimiento(contadorTurnos, nombreJugadorActual, fOrigen, cOrigen, fDestino, cDestino, accion));

                        cursorFila = fDestino;
                        cursorColumna = cDestino;

                        contadorTurnos++;
                        turnoJugador1 = !turnoJugador1;
                        Console.Clear();
                    }
                }
            }
        }
    }

    static void MoverCursor(ConsoleKey key, ref int f, ref int c)
    {
        if (key == ConsoleKey.UpArrow && f > 0) f--;
        else if (key == ConsoleKey.DownArrow && f < 7) f++;
        else if (key == ConsoleKey.LeftArrow && c > 0) c--;
        else if (key == ConsoleKey.RightArrow && c < 7) c++;
    }

    static void InicializarTablero()
    {
        for (int fila = 0; fila < 8; fila++)
        {
            for (int columna = 0; columna < 8; columna++)
            {
                if ((fila + columna) % 2 == 1)
                {
                    if (fila < 3) tablero[fila, columna] = 'O';
                    else if (fila > 4) tablero[fila, columna] = 'X';
                    else tablero[fila, columna] = '.';
                }
                else
                {
                    tablero[fila, columna] = ' ';
                }
            }
        }
    }

    static void DibujarTablero(int cursorFila = -1, int cursorColumna = -1, int origenFila = -1, int origenCol = -1)
    {
        Console.WriteLine("   0   1   2   3   4   5   6   7   ");
        Console.WriteLine(" ┌───┬───┬───┬───┬───┬───┬───┬───┐ ");

        for (int fila = 0; fila < 8; fila++)
        {
            Console.Write(fila + "│");
            for (int columna = 0; columna < 8; columna++)
            {
                char ficha = tablero[fila, columna];

                if (fila == origenFila && columna == origenCol)
                {
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else if (fila == cursorFila && columna == cursorColumna)
                {
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else if ((fila + columna) % 2 == 1)
                {
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                }

                Console.Write($" {ficha} ");
                Console.ResetColor();
                Console.Write("│");
            }
            Console.WriteLine();

            if (fila < 7)
                Console.WriteLine(" ├───┼───┼───┼───┼───┼───┼───┼───┤ ");
        }
        Console.WriteLine(" └───┴───┴───┴───┴───┴───┴───┴───┘ ");
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
        string resultado = string.Empty;

        // Evaluacion de victoria por tiempo agotado: Si a un jugador se le agotan sus 120 segundos del reloj global, gana el oponente por tiempo
        if (tiempoRestanteJ1 <= 0)
        {
            resultado = "Gana el Jugador 2 [O] (Jugador 1 agoto su tiempo total de 2 minutos)";
        }
        else if (tiempoRestanteJ2 <= 0)
        {
            resultado = "Gana el Jugador 1 [X] (Jugador 2 agoto su tiempo total de 2 minutos)";
        }
        else
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
                resultado = "Gana el Jugador 2 [O] (por eliminacion o acorralamiento)";
            else if (fichasJ2 == 0 || !movJ2)
                resultado = "Gana el Jugador 1 [X] (por eliminacion o acorralamiento)";
        }

        if (!string.IsNullOrEmpty(resultado))
        {
            Console.Clear();
            Console.WriteLine($"\n==================================================");
            Console.WriteLine($"JUEGO TERMINADO: {resultado}");
            Console.WriteLine($"==================================================");
            Console.WriteLine("\nPresiona cualquier tecla para continuar...");
            Console.ReadKey(true);
            return true;
        }

        return false;
    }

    static void MenuGuardarPartida()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("                       GUARDAR PARTIDA EN CURSO                           ");
        Console.WriteLine("==========================================================================");
        Console.WriteLine("Selecciona la ranura donde deseas guardar:");
        Console.WriteLine("  [1] Ranura 1 " + (File.Exists("partida_ranura_1.txt") ? "(Ocupada)" : "(Vacia)"));
        Console.WriteLine("  [2] Ranura 2 " + (File.Exists("partida_ranura_2.txt") ? "(Ocupada)" : "(Vacia)"));
        Console.WriteLine("  [3] Ranura 3 " + (File.Exists("partida_ranura_3.txt") ? "(Ocupada)" : "(Vacia)"));
        Console.WriteLine("  [4] Cancelar");
        Console.WriteLine("==========================================================================");
        Console.Write("Opcion (1-4): ");

        string op = Console.ReadLine() ?? "";
        if (op == "1" || op == "2" || op == "3")
        {
            GuardarEstadoArchivo($"partida_ranura_{op}.txt");
        }
    }

    static void GuardarEstadoArchivo(string nombreArchivo)
    {
        try
        {
            using (StreamWriter sw = new StreamWriter(nombreArchivo))
            {
                sw.WriteLine(turnoJugador1 ? "J1" : "J2");
                sw.WriteLine(contadorTurnos);
                sw.WriteLine(tiempoRestanteJ1);
                sw.WriteLine(tiempoRestanteJ2);

                for (int f = 0; f < 8; f++)
                {
                    string fila = "";
                    for (int c = 0; c < 8; c++)
                    {
                        fila += tablero[f, c];
                    }
                    sw.WriteLine(fila);
                }
            }
            MostrarMensaje($"¡Partida guardada con exito en {nombreArchivo}!");
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al guardar la partida: " + ex.Message);
        }
    }

    static bool CargarPartidaEnCurso()
    {
        Console.Clear();
        Console.WriteLine("==========================================================================");
        Console.WriteLine("                     CARGAR PARTIDA GUARDADA                              ");
        Console.WriteLine("==========================================================================");
        Console.WriteLine("Selecciona la ranura a cargar:");
        Console.WriteLine("  [1] Ranura 1 " + (File.Exists("partida_ranura_1.txt") ? "(Ocupada)" : "(Vacia)"));
        Console.WriteLine("  [2] Ranura 2 " + (File.Exists("partida_ranura_2.txt") ? "(Ocupada)" : "(Vacia)"));
        Console.WriteLine("  [3] Ranura 3 " + (File.Exists("partida_ranura_3.txt") ? "(Ocupada)" : "(Vacia)"));
        Console.WriteLine("  [4] Volver al Menu");
        Console.WriteLine("==========================================================================");
        Console.Write("Opcion (1-4): ");

        string op = Console.ReadLine() ?? "";
        if (op == "1" || op == "2" || op == "3")
        {
            string archivo = $"partida_ranura_{op}.txt";
            if (!File.Exists(archivo))
            {
                Console.WriteLine("\nEsa ranura esta vacia.");
                Console.WriteLine("Presiona cualquier tecla para continuar...");
                Console.ReadKey(true);
                return false;
            }

            try
            {
                string[] lineas = File.ReadAllLines(archivo);

                turnoJugador1 = (lineas[0] == "J1");
                contadorTurnos = int.Parse(lineas[1]);
                tiempoRestanteJ1 = double.Parse(lineas[2]);
                tiempoRestanteJ2 = double.Parse(lineas[3]);

                for (int f = 0; f < 8; f++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        tablero[f, c] = lineas[f + 4][c];
                    }
                }

                MostrarMensaje("¡Partida cargada exitosamente!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cargar la partida: " + ex.Message);
                Console.ReadKey(true);
                return false;
            }
        }

        return false;
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
        Thread.Sleep(1200);
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
            Console.WriteLine("                          [1] INICIAR NUEVA PARTIDA                       ");
            Console.WriteLine("                          [2] CARGAR PARTIDA GUARDADA                     ");
            Console.WriteLine("                          [3] VER REGLAS DEL JUEGO                        ");
            Console.WriteLine("                          [4] SALIR                                       ");
            Console.WriteLine();
            Console.WriteLine("==========================================================================");
            Console.Write("Selecciona una opcion (1-4): ");

            string opcion = Console.ReadLine() ?? string.Empty;

            if (opcion == "1")
            {
                InicializarTablero();
                historialMovimientos.Clear();
                contadorTurnos = 1;
                turnoJugador1 = true;
                tiempoRestanteJ1 = 120.0;
                tiempoRestanteJ2 = 120.0;
                Console.Clear();
                break;
            }
            else if (opcion == "2")
            {
                if (CargarPartidaEnCurso())
                {
                    Console.Clear();
                    break;
                }
            }
            else if (opcion == "3")
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
                Console.WriteLine("    - Las Damas pueden moverse y comer hacia adelante y hacia atras.");
                Console.WriteLine(" 8. Cada jugador tiene un reloj de ajedrez de 2 minutos (02:00) globales.");
                Console.WriteLine(" 9. Un jugador gana si elimina las fichas rivales, acorrala al oponente");
                Console.WriteLine("    o si al oponente se le agota el tiempo.");
                Console.WriteLine("==========================================================================");
                Console.WriteLine("\nPresiona cualquier tecla para volver al menu...");
                Console.ReadKey(true);
            }
            else if (opcion == "4")
            {
                Environment.Exit(0);
            }
        }
    }
}