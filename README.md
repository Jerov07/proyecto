Sistema de Gestión de Inventario
Este proyecto consiste en el desarrollo de un sistema de gestión de inventario para una tienda de ropa, el cual permite controlar productos, registrar movimientos y gestionar ventas en tiempo real.

El sistema ha sido mejorado para incluir concurrencia real, sincronización de procesos y gestión de memoria, simulando un entorno real con múltiples usuarios accediendo al sistema al mismo tiempo.

- Objetivo:
Desarrollar un sistema eficiente que permita:

Controlar el inventario en tiempo real
Ejecutar múltiples operaciones simultáneamente
Evitar errores por acceso concurrente
Analizar el impacto del uso de memoria en el rendimiento
- Características Principales:
Concurrencia Real
Uso de múltiples hilos (threading)
Simulación de múltiples usuarios:
Ventas simultáneas
Consultas concurrentes
Actualizaciones en tiempo real
* Sincronización
Implementación de Locks / Mutex
Protección de recursos compartidos (inventario)
Prevención de:
Condiciones de carrera
Inconsistencias en el stock
* Gestión de Memoria y Almacenamiento
Uso de estructuras en memoria (RAM) para rapidez
Persistencia en base de datos (SQLite/MySQL)
Balance entre rendimiento y almacenamiento
* Registro de Evidencias
El sistema genera archivos que evidencian su ejecución:

log.txt
Contiene:

Fecha y hora
Hilo ejecutando la acción
Tipo de operación
Resultado
Ejemplo:

[10:05:21] Hilo-1: Venta realizada - Camiseta Negra Talla M
[10:05:22] Hilo-2: Error - Stock insuficiente
config.json
Contiene:

Número de hilos
Configuración del sistema
Parámetros de simulación
Estructura del Proyecto
Sistema-inventario/
│
├── main.py
├── inventario.py
├── ventas.py
├── concurrencia.py
├── config.json
├── log.txt
└── README.md
- Cómo Ejecutar el Proyecto
Clonar el repositorio:
git clone https://github.com/tu-usuario/sistema-inventario.git
Entrar a la carpeta:
cd sistema-inventario
Ejecutar el programa:
python main.py
- Evidencia de Concurrencia
El sistema puede ejecutar múltiples hilos al mismo tiempo:

5 hilos realizando ventas
3 hilos consultando inventario
Esto demuestra ejecución concurrente real.

- Reglas del Sistema
No se puede vender si no hay stock
El inventario nunca puede ser negativo
Cada operación queda registrada en logs
Solo procesos sincronizados pueden modificar el inventario
° Tecnologías Utilizadas
° Python
° threading
° SQLite / MySQL
° Archivos de log
° Git
° Resultados
Reducción de errores en inventario
Mayor eficiencia en operaciones simultáneas
Sistema más realista y escalable
- Conclusión
Este proyecto no solo resuelve un problema real de inventario, sino que también implementa conceptos clave como:

Concurrencia
Sincronización
Manejo de memoria
Evidencia de ejecución
Lo que lo convierte en una solución completa y alineada con entornos reales de software.
