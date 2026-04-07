using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace SistemaInventarioTienda
{
    class Program
    {
        static List<Producto> productos = new List<Producto>();
        static List<Inventario> inventarios = new List<Inventario>();

        static readonly object lockObj = new object();

        static string logFile = "log.txt";
        static string configFile = "config.txt";

        static bool usarDisco = false;

        static void Main(string[] args)
        {
            CargarConfiguracion();
            File.WriteAllText(logFile, "===== INICIO DE EJECUCIÓN =====\n");

            int opcion;

            do
            {
                Console.Clear();
                Console.WriteLine("=====================================");
                Console.WriteLine(" SISTEMA DE INVENTARIO - CONCURRENCIA ");
                Console.WriteLine("=====================================");
                Console.WriteLine("1. Registrar Producto");
                Console.WriteLine("2. Agregar Inventario");
                Console.WriteLine("3. Simular Ventas Concurrentes");
                Console.WriteLine("4. Ver Inventario");
                Console.WriteLine("5. Salir");
                Console.Write("Seleccione una opción: ");

                if (!int.TryParse(Console.ReadLine(), out opcion))
                {
                    Console.WriteLine("❌ Opción inválida");
                    Console.ReadKey();
                    continue;
                }

                switch (opcion)
                {
                    case 1: RegistrarProducto(); break;
                    case 2: AgregarInventario(); break;
                    case 3: SimularVentasConcurrentes(); break;
                    case 4: VerInventario(); break;
                }

                Console.WriteLine("\nPresione una tecla para continuar...");
                Console.ReadKey();

            } while (opcion != 5);
        }

        // ================= CONFIG =================

        static void CargarConfiguracion()
        {
            if (File.Exists(configFile))
            {
                var config = File.ReadAllText(configFile).ToUpper();
                usarDisco = config.Contains("DISCO");
            }
            else
            {
                File.WriteAllText(configFile, "MODO=RAM");
                usarDisco = false;
            }
        }

        static void Log(string mensaje)
        {
            lock (lockObj)
            {
                File.AppendAllText(logFile, $"{DateTime.Now:HH:mm:ss.fff} - {mensaje}\n");
            }
        }

        // ================= PRODUCTO =================

        static void RegistrarProducto()
        {
            Console.Clear();
            Console.WriteLine("----- REGISTRAR PRODUCTO -----");

            int id;
            Console.Write("ID: ");
            while (!int.TryParse(Console.ReadLine(), out id))
                Console.Write("Ingrese un número válido: ");

            lock (lockObj)
            {
                if (productos.Any(p => p.Id == id))
                {
                    Console.WriteLine("❌ Ya existe ese ID.");
                    return;
                }
            }

            Console.Write("Nombre: ");
            string nombre = Console.ReadLine();

            decimal precio;
            Console.Write("Precio: ");
            while (!decimal.TryParse(Console.ReadLine(), out precio))
                Console.Write("Ingrese un precio válido: ");

            lock (lockObj)
            {
                productos.Add(new Producto { Id = id, Nombre = nombre, Precio = precio });
            }

            Log($"Producto registrado: {nombre}");
            Console.WriteLine("✔ Producto registrado.");
        }

        // ================= INVENTARIO =================

        static void AgregarInventario()
        {
            Console.Clear();
            Console.WriteLine("----- AGREGAR INVENTARIO -----");

            int id;
            Console.Write("ID Producto: ");
            while (!int.TryParse(Console.ReadLine(), out id))
                Console.Write("Ingrese un número válido: ");

            Producto producto;

            lock (lockObj)
            {
                producto = productos.FirstOrDefault(p => p.Id == id);
            }

            if (producto == null)
            {
                Console.WriteLine("❌ Producto no existe.");
                return;
            }

            Console.Write("Talla: ");
            string talla = Console.ReadLine();

            Console.Write("Color: ");
            string color = Console.ReadLine();

            int cantidad;
            Console.Write("Cantidad: ");
            while (!int.TryParse(Console.ReadLine(), out cantidad))
                Console.Write("Ingrese un número válido: ");

            int stockMin;
            Console.Write("Stock mínimo: ");
            while (!int.TryParse(Console.ReadLine(), out stockMin))
                Console.Write("Ingrese un número válido: ");

            lock (lockObj)
            {
                var existente = inventarios.FirstOrDefault(i =>
                    i.Producto.Id == id &&
                    i.Talla == talla &&
                    i.Color == color);

                if (existente != null)
                {
                    existente.Cantidad += cantidad;
                }
                else
                {
                    inventarios.Add(new Inventario
                    {
                        Producto = producto,
                        Talla = talla,
                        Color = color,
                        Cantidad = cantidad,
                        StockMinimo = stockMin
                    });
                }
            }

            Log($"Inventario agregado: {producto.Nombre}");
            Console.WriteLine("✔ Inventario actualizado.");
        }

        // ================= CONCURRENCIA =================

        static void SimularVentasConcurrentes()
        {
            Console.Clear();
            Console.WriteLine("Simulando ventas concurrentes...\n");

            var tareas = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                tareas.Add(Task.Run(() => RealizarVentaConcurrente()));
            }

            Task.WaitAll(tareas.ToArray());

            Console.WriteLine("\n✔ Simulación finalizada.");
        }

        static void RealizarVentaConcurrente()
        {
            var random = new Random(Guid.NewGuid().GetHashCode());

            List<Inventario> copia;

            lock (lockObj)
            {
                copia = inventarios.ToList();
            }

            foreach (var item in copia)
            {
                int cantidadVenta = random.Next(1, 3);

                lock (lockObj)
                {
                    if (item.Cantidad >= cantidadVenta)
                    {
                        item.Cantidad -= cantidadVenta;

                        Log($"Venta: {item.Producto.Nombre} - {cantidadVenta}");

                        if (usarDisco)
                        {
                            File.AppendAllText("ventas.txt",
                                $"{item.Producto.Nombre},{cantidadVenta}\n");

                            Thread.Sleep(50); // simula lentitud de disco
                        }
                    }
                    else
                    {
                        Log($"Stock insuficiente: {item.Producto.Nombre}");
                    }
                }
            }
        }

        // ================= VER INVENTARIO =================

        static void VerInventario()
        {
            Console.Clear();
            Console.WriteLine("----- INVENTARIO ACTUAL -----");

            List<Inventario> copia;

            lock (lockObj)
            {
                copia = inventarios
                    .Select(i => new Inventario
                    {
                        Producto = i.Producto,
                        Talla = i.Talla,
                        Color = i.Color,
                        Cantidad = i.Cantidad,
                        StockMinimo = i.StockMinimo
                    })
                    .ToList();
            }

            if (copia.Count == 0)
            {
                Console.WriteLine("No hay inventario.");
                return;
            }

            foreach (var item in copia)
            {
                Console.WriteLine($"Producto: {item.Producto.Nombre} (ID: {item.Producto.Id})");
                Console.WriteLine($"Talla: {item.Talla}");
                Console.WriteLine($"Color: {item.Color}");
                Console.WriteLine($"Cantidad: {item.Cantidad}");

                if (item.Cantidad <= item.StockMinimo)
                    Console.WriteLine("⚠ Bajo stock");

                Console.WriteLine("---------------------------");
            }
        }
    }

    // ================= CLASES =================

    class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
    }

    class Inventario
    {
        public Producto Producto { get; set; }
        public string Talla { get; set; }
        public string Color { get; set; }
        public int Cantidad { get; set; }
        public int StockMinimo { get; set; }
    }
}
