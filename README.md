# Kiosko People Tech · Videojuegos (Commit Crew)

Minijuegos en **Unity** que forman parte del Kiosko People Tech. Cada juego se compila para **WebGL**, se juega dentro del portal web y se conecta al backend **kiosko-api** para validar turnos, obtener preguntas de la base de datos, registrar puntajes y entregar monedas al jugador.

| Juego | Carpeta en el proyecto | Mecánica |
|---|---|---|
| Head Breaker | `Assets/_Project/Scenes/Headbreaker` | Rompecabezas con imágenes administradas desde el portal |
| Orbit Payment | `Assets/_Project/Scenes/OrbitPayment` | Plataformas y quiz sobre procesos de pago |
| BrewStack | `Assets/_Project/Scenes/BrewStack` | Torre de bloques con trivia antes de cada bloque |
| Atlas Express | `Assets/_Project/Scenes/AtlasExpress` | Clasificar documentos en la carpeta del área correcta |
| Space Defender | `Assets/_Project/Scenes/SpaceDefender` | Shooter de oleadas con preguntas para recuperar vida |

## Requisitos

- [Unity Hub](https://unity.com/download).
- **Unity 6000.4.0f1** con el módulo **WebGL Build Support** instalado.

## Abrir los proyectos

1. Clona el repositorio:
   ```bash
   git clone https://github.com/ringo-afk/Escuadra-de-VJ.git
   ```
2. En Unity Hub elige **Add → Add project from disk** y selecciona la carpeta raíz del repositorio. Ahí están los cinco juegos.
3. La primera vez Unity regenera la carpeta `Library` (puede tardar varios minutos).
4. Abre la escena del juego en `Assets/_Project/Scenes/<Juego>/` (por ejemplo `Headbreaker/HB_Scenes/HB_MainMenu.unity`) y presiona **Play**. Dentro del editor, los juegos usan `http://localhost:3000` como API y un usuario de prueba configurado en su script de integración, así que conviene tener encendido **kiosko-api**.

## Compilar para WebGL

1. **File → Build Profiles → Web**, agrega las escenas del juego y selecciona **Build**.
2. Copia el contenido generado a `kiosko-api/public/juegos/<juego>/` (`headbreaker`, `orbitpayment`, `brewstack`, `atlasexpress` o `spacedefender`).
3. Con el API encendido, el juego queda disponible en `http://localhost:3000/juegos/<juego>/index.html?usuario=<id>` y en la sección **Mini Juegos** del portal.

Los builds ya compilados están en `WebGL_Builds/` y en `kiosko-api/public/juegos/`.

## Conexión con el kiosko

Cada juego tiene un script de integración (`BrewKiosko.cs`, `OrbitKiosko.cs`, `AtlasKiosko.cs`, `SDKiosko.cs` y, en Head Breaker, los scripts de `HB_Scripts/Kiosko`) que:

1. Lee el id del usuario desde la URL (`?usuario=`).
2. Llama `POST /api/partidas/iniciar` para validar que tenga turno disponible.
3. Descarga las preguntas o la configuración del juego (`GET /api/preguntas`, `/api/trivia`, `/api/atlas/documentos`, `/api/headbreaker/config`).
4. Al terminar, registra el resultado con `POST /api/partidas`; la base de datos suma las monedas al monedero del jugador.

Las peticiones se hacen con `UnityWebRequest` al mismo servidor que sirve el juego, por lo que no hay problemas de CORS.

## Equipo Commit Crew

Alejandro Torruco · Ringo García · Grecia Archuleta · César Higareda · Rogelio Peña
