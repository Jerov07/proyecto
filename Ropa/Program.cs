using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace TiendaRopa
{
    // ══════════════════════════════════════════════════════════
    //  MODELOS DE DATOS
    // ══════════════════════════════════════════════════════════

    class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "";
        public double Precio { get; set; }
        public int Cantidad { get; set; }

        public Producto() { }
        public Producto(int id, string nombre, string categoria, double precio, int cantidad)
        {
            Id = id; Nombre = nombre; Categoria = categoria;
            Precio = precio; Cantidad = cantidad;
        }

        public override string ToString() =>
            $"  [{Id:D3}] {Nombre,-30} | ${Precio,10:N0} | Stock: {Cantidad}";
    }

    class Vendedor
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = "";
        public string Contrasena { get; set; } = "";
        public string Nombre { get; set; } = "";

        public Vendedor() { }
        public Vendedor(int id, string usuario, string contrasena, string nombre)
        {
            Id = id; Usuario = usuario; Contrasena = contrasena; Nombre = nombre;
        }
    }

    class ItemVenta
    {
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = "";
        public double PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public double Subtotal => PrecioUnitario * Cantidad;
    }

    class Venta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int VendedorId { get; set; }
        public string VendedorNombre { get; set; } = "";
        public List<ItemVenta> Items { get; set; } = new();
        public double Total => Items.Sum(i => i.Subtotal);
        public string NumeroFactura => $"FAC-{Id:D5}";
    }

    // ══════════════════════════════════════════════════════════
    //  PERSISTENCIA EN DISCO (JSON)
    // ══════════════════════════════════════════════════════════

    static class Persistencia
    {
        static readonly string Directorio = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string ArchivoInventario = Path.Combine(Directorio, "inventario.json");
        static readonly string ArchivoVentas = Path.Combine(Directorio, "ventas.json");
        static readonly string ArchivoContador = Path.Combine(Directorio, "contador.json");

        static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

        public static void GuardarInventario(List<Producto> lista)
        {
            File.WriteAllText(ArchivoInventario, JsonSerializer.Serialize(lista, Opts));
        }

        public static List<Producto>? CargarInventario()
        {
            if (!File.Exists(ArchivoInventario)) return null;
            return JsonSerializer.Deserialize<List<Producto>>(File.ReadAllText(ArchivoInventario));
        }

        public static void GuardarVentas(List<Venta> lista, int contador)
        {
            File.WriteAllText(ArchivoVentas, JsonSerializer.Serialize(lista, Opts));
            File.WriteAllText(ArchivoContador, contador.ToString());
        }

        public static (List<Venta> ventas, int contador) CargarVentas()
        {
            var ventas = new List<Venta>();
            int contador = 1;
            if (File.Exists(ArchivoVentas))
                ventas = JsonSerializer.Deserialize<List<Venta>>(File.ReadAllText(ArchivoVentas)) ?? ventas;
            if (File.Exists(ArchivoContador) && int.TryParse(File.ReadAllText(ArchivoContador), out int c))
                contador = c;
            return (ventas, contador);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  BASE DE DATOS INTERNA
    // ══════════════════════════════════════════════════════════

    static class BaseDeDatos
    {
        public static List<Vendedor> Vendedores = new()
        {
            new Vendedor(1, "carlos01",  "pass123",   "Carlos Martínez"),
            new Vendedor(2, "laura02",   "ropa456",   "Laura Gómez"),
            new Vendedor(3, "andres03",  "tienda789", "Andrés López"),
        };

        static readonly List<Producto> InventarioDefault = new()
        {
            new Producto(101, "Camiseta Básica Blanca",     "Camisetas",  25_000, 30),
            new Producto(102, "Camiseta Polo Negra",         "Camisetas",  45_000, 20),
            new Producto(103, "Camiseta Estampada Tropical", "Camisetas",  38_000, 15),
            new Producto(104, "Camiseta Oversize Gris",      "Camisetas",  42_000, 18),
            new Producto(201, "Jean Skinny Azul",            "Pantalones", 85_000, 25),
            new Producto(202, "Jean Straight Negro",         "Pantalones", 90_000, 20),
            new Producto(203, "Pantalón Cargo Caqui",        "Pantalones", 75_000, 12),
            new Producto(204, "Jogger Gris Jaspe",           "Pantalones", 60_000, 16),
            new Producto(301, "Tenis Deportivos Blancos",    "Zapatos",   120_000, 10),
            new Producto(302, "Zapato Cuero Café",           "Zapatos",   180_000,  8),
            new Producto(303, "Sandalia Casual Negra",       "Zapatos",    65_000, 14),
            new Producto(304, "Bota de Cuero Negra",         "Zapatos",   210_000,  6),
            new Producto(401, "Chaqueta Bomber Verde",       "Chaquetas", 150_000, 10),
            new Producto(402, "Chaqueta Jean Azul",          "Chaquetas", 130_000, 12),
            new Producto(403, "Hoodie Gris con Cremallera",  "Chaquetas",  95_000, 20),
            new Producto(501, "Gorra Negra Snapback",        "Accesorios", 35_000, 25),
            new Producto(502, "Cinturón de Cuero Café",      "Accesorios", 28_000, 30),
            new Producto(503, "Bolso Tote Canvas Beige",     "Accesorios", 55_000, 15),
        };

        public static List<Producto> Inventario = new();
        public static List<Venta> Ventas = new();
        private static int _contadorVentas = 1;

        public static void Inicializar()
        {
            // Inventario: cargar del disco o usar valores por defecto
            var inventarioCargado = Persistencia.CargarInventario();
            Inventario = inventarioCargado ?? InventarioDefault;

            // Ventas: cargar del disco
            var (ventas, contador) = Persistencia.CargarVentas();
            Ventas = ventas;
            _contadorVentas = contador;
        }

        public static int SiguienteIdVenta() => _contadorVentas++;

        public static void Guardar()
        {
            Persistencia.GuardarInventario(Inventario);
            Persistencia.GuardarVentas(Ventas, _contadorVentas);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  UTILIDADES DE CONSOLA
    // ══════════════════════════════════════════════════════════

    static class UI
    {
        public static void Titulo(string texto)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔" + new string('═', 58) + "╗");
            Console.WriteLine("║  " + texto.PadRight(56) + "║");
            Console.WriteLine("╚" + new string('═', 58) + "╝");
            Console.ResetColor();
        }

        public static void SubTitulo(string texto)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  ── " + texto + " ──");
            Console.ResetColor();
        }

        public static void Exito(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✔ " + msg);
            Console.ResetColor();
        }

        public static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✖ " + msg);
            Console.ResetColor();
        }

        public static void Info(string msg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  " + msg);
            Console.ResetColor();
        }

        public static void Advertencia(string msg)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("  ⚠ " + msg);
            Console.ResetColor();
        }

        public static void Linea() =>
            Console.WriteLine("  " + new string('─', 56));

        public static string Leer(string prompt)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  > " + prompt + ": ");
            Console.ResetColor();
            return Console.ReadLine()?.Trim() ?? "";
        }

        public static string LeerContrasena(string prompt)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  > " + prompt + ": ");
            Console.ResetColor();
            var pass = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                { pass += key.KeyChar; Console.Write("*"); }
                else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                { pass = pass[..^1]; Console.Write("\b \b"); }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pass;
        }

        public static void Pausa()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n  Presione cualquier tecla para continuar...");
            Console.ResetColor();
            Console.ReadKey(true);
        }

        public static bool Confirmar(string pregunta)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"\n  ¿{pregunta}? (s/n): ");
            Console.ResetColor();
            var r = Console.ReadLine()?.Trim().ToLower();
            return r == "s" || r == "si" || r == "sí";
        }

        public static void Limpiar() => Console.Clear();
    }

    // ══════════════════════════════════════════════════════════
    //  MÓDULO: INVENTARIO
    // ══════════════════════════════════════════════════════════

    static class ModuloInventario
    {
        public static void MostrarInventarioPublico()
        {
            UI.Limpiar();
            UI.Titulo("INVENTARIO — TIENDA DE ROPA");
            var categorias = BaseDeDatos.Inventario.Select(p => p.Categoria).Distinct().OrderBy(c => c);
            foreach (var cat in categorias)
            {
                UI.SubTitulo(cat);
                foreach (var p in BaseDeDatos.Inventario.Where(x => x.Categoria == cat))
                    UI.Info(p.ToString());
            }
            UI.Pausa();
        }

        public static void MostrarCategoria(string categoria)
        {
            var productos = BaseDeDatos.Inventario
                .Where(p => p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase) && p.Cantidad > 0)
                .ToList();

            if (!productos.Any()) { UI.Error("No hay productos disponibles en esa categoría."); return; }

            UI.SubTitulo($"Productos disponibles — {categoria}");
            Console.WriteLine($"  {"ID",-6} {"Nombre",-30} {"Precio",12}  {"Stock",6}");
            UI.Linea();
            foreach (var p in productos)
                UI.Info($"[{p.Id:D3}]  {p.Nombre,-30}  ${p.Precio,10:N0}  ({p.Cantidad} uds)");
        }
    }

    // ══════════════════════════════════════════════════════════
    //  MÓDULO: VENTAS
    // ══════════════════════════════════════════════════════════

    static class ModuloVentas
    {
        static readonly string[] Categorias =
            { "Camisetas", "Pantalones", "Zapatos", "Chaquetas", "Accesorios" };

        public static void RealizarVenta(Vendedor vendedor)
        {
            UI.Limpiar();
            UI.Titulo("NUEVA VENTA");

            var items = new List<ItemVenta>();
            bool agregarMas = true;

            while (agregarMas)
            {
                UI.SubTitulo("¿Qué tipo de producto desea comprar el cliente?");
                for (int i = 0; i < Categorias.Length; i++)
                    UI.Info($"  {i + 1}. {Categorias[i]}");
                UI.Linea();

                var entradaCat = UI.Leer("Seleccione categoría (número)");
                if (!int.TryParse(entradaCat, out int numCat) || numCat < 1 || numCat > Categorias.Length)
                { UI.Error("Opción inválida."); UI.Pausa(); continue; }

                string categoriaElegida = Categorias[numCat - 1];
                ModuloInventario.MostrarCategoria(categoriaElegida);

                var entradaId = UI.Leer("Ingrese el ID del producto");
                if (!int.TryParse(entradaId, out int idProd))
                { UI.Error("ID inválido."); UI.Pausa(); continue; }

                var producto = BaseDeDatos.Inventario
                    .FirstOrDefault(p => p.Id == idProd &&
                        p.Categoria.Equals(categoriaElegida, StringComparison.OrdinalIgnoreCase));

                if (producto == null) { UI.Error("Producto no encontrado en esa categoría."); UI.Pausa(); continue; }
                if (producto.Cantidad == 0) { UI.Error("Ese producto no tiene stock disponible."); UI.Pausa(); continue; }

                var entradaCant = UI.Leer($"Cantidad de '{producto.Nombre}'");
                if (!int.TryParse(entradaCant, out int cant) || cant <= 0)
                { UI.Error("Cantidad inválida."); UI.Pausa(); continue; }
                if (cant > producto.Cantidad)
                { UI.Error($"Stock insuficiente. Disponible: {producto.Cantidad}"); UI.Pausa(); continue; }

                var itemExistente = items.FirstOrDefault(i => i.ProductoId == idProd);
                if (itemExistente != null)
                {
                    var prodRef = BaseDeDatos.Inventario.First(p => p.Id == idProd);
                    if (itemExistente.Cantidad + cant > prodRef.Cantidad)
                    { UI.Error("No hay suficiente stock para esa cantidad adicional."); UI.Pausa(); continue; }
                    itemExistente.Cantidad += cant;
                }
                else
                {
                    items.Add(new ItemVenta
                    {
                        ProductoId = producto.Id,
                        ProductoNombre = producto.Nombre,
                        PrecioUnitario = producto.Precio,
                        Cantidad = cant
                    });
                }

                UI.Exito($"'{producto.Nombre}' x{cant} agregado.");
                UI.Linea();
                agregarMas = UI.Confirmar("¿El cliente desea comprar otro producto?");
            }

            if (!items.Any())
            { UI.Advertencia("Venta cancelada: no se agregaron productos."); UI.Pausa(); return; }

            MostrarResumenVenta(items);
            double total = items.Sum(i => i.Subtotal);

            // Cobro con 3 intentos
            bool pagoCorrecto = false;
            for (int intento = 1; intento <= 3; intento++)
            {
                var entradaPago = UI.Leer($"Monto recibido del cliente (Total: ${total:N0}) — Intento {intento}/3");
                if (!double.TryParse(entradaPago, out double pagado))
                { UI.Error("Monto inválido."); continue; }

                if (pagado >= total)
                {
                    UI.Exito($"Pago aceptado. Cambio: ${pagado - total:N0}");
                    pagoCorrecto = true;
                    break;
                }
                else
                {
                    UI.Error($"Monto insuficiente. Faltan: ${total - pagado:N0}");
                    if (intento < 3) UI.Advertencia($"Le quedan {3 - intento} intento(s).");
                }
            }

            if (!pagoCorrecto)
            { UI.Error("El cliente no completó el pago. Venta CANCELADA — no se registrará."); UI.Pausa(); return; }

            if (!UI.Confirmar("¿Confirmar y guardar la venta?"))
            { UI.Advertencia("Venta descartada — no se registrará."); UI.Pausa(); return; }

            // Persistir
            var venta = new Venta
            {
                Id = BaseDeDatos.SiguienteIdVenta(),
                Fecha = DateTime.Now,
                VendedorId = vendedor.Id,
                VendedorNombre = vendedor.Nombre,
                Items = items
            };

            // Descontar inventario
            foreach (var item in items)
            {
                var prod = BaseDeDatos.Inventario.First(p => p.Id == item.ProductoId);
                prod.Cantidad -= item.Cantidad;
            }

            BaseDeDatos.Ventas.Add(venta);
            BaseDeDatos.Guardar(); // 💾 GUARDAR EN DISCO

            ImprimirFactura(venta);
            UI.Exito("Venta guardada correctamente en disco.");
            UI.Pausa();
        }

        static void MostrarResumenVenta(List<ItemVenta> items)
        {
            UI.SubTitulo("Resumen de la venta");
            Console.WriteLine($"  {"Producto",-30} {"Cant",5}  {"Precio Unit",12}  {"Subtotal",12}");
            UI.Linea();
            foreach (var i in items)
                UI.Info($"  {i.ProductoNombre,-30} {i.Cantidad,5}  ${i.PrecioUnitario,10:N0}  ${i.Subtotal,10:N0}");
            UI.Linea();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  {"TOTAL",-30}{"",5}  {"",12}  ${items.Sum(x => x.Subtotal),10:N0}");
            Console.ResetColor();
        }

        static void ImprimirFactura(Venta v)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║                  TIENDA DE ROPA S.A.S               ║");
            Console.WriteLine("  ║               NIT: 900.123.456-7  Medellín          ║");
            Console.WriteLine("  ╠══════════════════════════════════════════════════════╣");
            Console.WriteLine($"  ║  Factura : {v.NumeroFactura,-42}║");
            Console.WriteLine($"  ║  Fecha   : {v.Fecha:dd/MM/yyyy HH:mm:ss,-42}║");
            Console.WriteLine($"  ║  Vendedor: {v.VendedorNombre,-42}║");
            Console.WriteLine("  ╠══════════════════════════════════════════════════════╣");
            Console.ResetColor();
            foreach (var item in v.Items)
            {
                string linea = $"  {item.ProductoNombre} x{item.Cantidad}  →  ${item.Subtotal:N0}";
                Console.WriteLine($"  ║  {linea,-54}║");
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ╠══════════════════════════════════════════════════════╣");
            Console.WriteLine($"  ║  TOTAL A PAGAR: ${v.Total,37:N0} ║");
            Console.WriteLine("  ╠══════════════════════════════════════════════════════╣");
            Console.WriteLine("  ║       ¡Gracias por su compra! Vuelva pronto.         ║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        public static void VerHistorialVentas()
        {
            UI.Limpiar();
            UI.Titulo("HISTORIAL DE VENTAS");

            if (!BaseDeDatos.Ventas.Any())
            { UI.Info("No hay ventas registradas aún."); UI.Pausa(); return; }

            foreach (var v in BaseDeDatos.Ventas)
            {
                UI.SubTitulo($"{v.NumeroFactura}  —  {v.Fecha:dd/MM/yyyy HH:mm}  —  {v.VendedorNombre}");
                foreach (var i in v.Items)
                    UI.Info($"    {i.ProductoNombre} x{i.Cantidad}  →  ${i.Subtotal:N0}");
                UI.Info($"    TOTAL: ${v.Total:N0}");
            }
            UI.Pausa();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  MÓDULO: EDICIÓN DE INVENTARIO (clave 1808)
    // ══════════════════════════════════════════════════════════

    static class ModuloEdicion
    {
        const string ClaveEdicion = "1808";

        public static void Acceder()
        {
            UI.Limpiar();
            UI.Titulo("EDITAR INVENTARIO — ACCESO RESTRINGIDO");
            var clave = UI.LeerContrasena("Ingrese la clave de administración");
            if (clave != ClaveEdicion) { UI.Error("Clave incorrecta. Acceso denegado."); UI.Pausa(); return; }
            MenuEdicion();
        }

        static void MenuEdicion()
        {
            bool enMenu = true;
            while (enMenu)
            {
                UI.Limpiar();
                UI.Titulo("EDITAR INVENTARIO");
                UI.Info("  1. Editar cantidad de producto");
                UI.Info("  2. Editar nombre de producto");
                UI.Info("  3. Editar precio de producto");
                UI.Info("  4. Regresar al menú del vendedor");
                UI.Info("  5. Salir del programa");
                UI.Linea();

                var op = UI.Leer("Seleccione una opción");
                switch (op)
                {
                    case "1": EditarCantidad(); break;
                    case "2": EditarNombre(); break;
                    case "3": EditarPrecio(); break;
                    case "4":
                        if (UI.Confirmar("¿Desea regresar al menú del vendedor?"))
                            enMenu = false;
                        break;
                    case "5":
                        if (UI.Confirmar("¿Desea cerrar el programa por completo?"))
                        { CerrarPrograma(); }
                        break;
                    default:
                        UI.Error("Opción inválida."); UI.Pausa(); break;
                }
            }
        }

        static Producto? BuscarProducto()
        {
            ModuloInventario.MostrarInventarioPublico();
            var entId = UI.Leer("Ingrese el ID del producto a editar");
            if (!int.TryParse(entId, out int id)) { UI.Error("ID inválido."); UI.Pausa(); return null; }
            var p = BaseDeDatos.Inventario.FirstOrDefault(x => x.Id == id);
            if (p == null) { UI.Error("Producto no encontrado."); UI.Pausa(); }
            return p;
        }

        static void EditarCantidad()
        {
            UI.Limpiar(); UI.Titulo("EDITAR CANTIDAD");
            var p = BuscarProducto(); if (p == null) return;
            UI.Info($"Producto: {p.Nombre}  |  Cantidad actual: {p.Cantidad}");
            var ent = UI.Leer("Nueva cantidad");
            if (!int.TryParse(ent, out int nueva) || nueva < 0) { UI.Error("Valor inválido."); UI.Pausa(); return; }
            p.Cantidad = nueva;
            BaseDeDatos.Guardar(); // 💾
            UI.Exito($"Cantidad actualizada a {nueva} y guardada."); UI.Pausa();
        }

        static void EditarNombre()
        {
            UI.Limpiar(); UI.Titulo("EDITAR NOMBRE");
            var p = BuscarProducto(); if (p == null) return;
            UI.Info($"Nombre actual: {p.Nombre}");
            var nuevo = UI.Leer("Nuevo nombre");
            if (string.IsNullOrWhiteSpace(nuevo)) { UI.Error("Nombre inválido."); UI.Pausa(); return; }
            p.Nombre = nuevo;
            BaseDeDatos.Guardar(); // 💾
            UI.Exito("Nombre actualizado y guardado."); UI.Pausa();
        }

        static void EditarPrecio()
        {
            UI.Limpiar(); UI.Titulo("EDITAR PRECIO");
            var p = BuscarProducto(); if (p == null) return;
            UI.Info($"Producto: {p.Nombre}  |  Precio actual: ${p.Precio:N0}");
            var ent = UI.Leer("Nuevo precio");
            if (!double.TryParse(ent, out double nuevo) || nuevo <= 0) { UI.Error("Precio inválido."); UI.Pausa(); return; }
            p.Precio = nuevo;
            BaseDeDatos.Guardar(); // 💾
            UI.Exito($"Precio actualizado a ${nuevo:N0} y guardado."); UI.Pausa();
        }

        static void CerrarPrograma()
        {
            UI.Limpiar();
            UI.Info("Guardando datos y cerrando el sistema. ¡Hasta pronto!");
            BaseDeDatos.Guardar();
            Thread.Sleep(800);
            Environment.Exit(0);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  MÓDULO: MENÚ VENDEDOR
    // ══════════════════════════════════════════════════════════

    static class ModuloVendedor
    {
        public static void Menu(Vendedor vendedor)
        {
            bool sesionActiva = true;
            while (sesionActiva)
            {
                UI.Limpiar();
                UI.Titulo($"MENÚ VENDEDOR — {vendedor.Nombre}");
                UI.Info("  1. Realizar venta");
                UI.Info("  2. Ver historial de ventas");
                UI.Info("  3. Ver inventario");
                UI.Info("  4. Editar inventario");
                UI.Info("  5. Cerrar sesión");
                UI.Info("  6. Salir del programa");
                UI.Linea();

                var op = UI.Leer("Seleccione una opción");
                switch (op)
                {
                    case "1": ModuloVentas.RealizarVenta(vendedor); break;
                    case "2": ModuloVentas.VerHistorialVentas(); break;
                    case "3": ModuloInventario.MostrarInventarioPublico(); break;
                    case "4": ModuloEdicion.Acceder(); break;
                    case "5":
                        if (UI.Confirmar("¿Desea cerrar la sesión?"))
                            sesionActiva = false;
                        break;
                    case "6":
                        if (UI.Confirmar("¿Desea salir del programa por completo?"))
                        {
                            UI.Limpiar();
                            UI.Info("Guardando datos y cerrando. ¡Hasta pronto!");
                            BaseDeDatos.Guardar();
                            Thread.Sleep(800);
                            Environment.Exit(0);
                        }
                        break;
                    default:
                        UI.Error("Opción inválida."); UI.Pausa(); break;
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  MÓDULO: LOGIN
    // ══════════════════════════════════════════════════════════

    static class ModuloLogin
    {
        public static Vendedor? IniciarSesion()
        {
            UI.Limpiar();
            UI.Titulo("INICIO DE SESIÓN — VENDEDOR");

            for (int intento = 1; intento <= 3; intento++)
            {
                UI.Info($"\n  Intento {intento} de 3");
                var usuario = UI.Leer("Usuario");
                var contrasena = UI.LeerContrasena("Contraseña");

                var vendedor = BaseDeDatos.Vendedores
                    .FirstOrDefault(v => v.Usuario == usuario && v.Contrasena == contrasena);

                if (vendedor != null)
                {
                    UI.Exito($"Bienvenido, {vendedor.Nombre}!");
                    Thread.Sleep(900);
                    return vendedor;
                }

                UI.Error("Usuario o contraseña incorrectos.");
                if (intento < 3) UI.Advertencia($"Le quedan {3 - intento} intento(s).");
            }

            UI.Error("\nAcceso bloqueado: demasiados intentos fallidos.");
            UI.Pausa();
            return null;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  PANTALLA PRINCIPAL
    // ══════════════════════════════════════════════════════════

    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Tienda de Ropa — Sistema de Inventario";

            BaseDeDatos.Inicializar(); // Carga datos del disco al arrancar

            bool corriendo = true;
            while (corriendo)
            {
                UI.Limpiar();
                UI.Titulo("TIENDA DE ROPA — SISTEMA DE INVENTARIO");
                Console.WriteLine();
                UI.Info("  1. Iniciar sesión (Vendedor)");
                UI.Info("  2. Ver inventario (Público)");
                UI.Info("  3. Salir");
                UI.Linea();

                var op = UI.Leer("Seleccione una opción");
                switch (op)
                {
                    case "1":
                        var vendedor = ModuloLogin.IniciarSesion();
                        if (vendedor != null) ModuloVendedor.Menu(vendedor);
                        break;
                    case "2":
                        ModuloInventario.MostrarInventarioPublico();
                        break;
                    case "3":
                        if (UI.Confirmar("¿Desea cerrar el programa?"))
                        {
                            UI.Limpiar();
                            UI.Info("Guardando datos. ¡Hasta pronto!");
                            BaseDeDatos.Guardar();
                            corriendo = false;
                        }
                        break;
                    default:
                        UI.Error("Opción inválida."); UI.Pausa(); break;
                }
            }
        }
    }
}
