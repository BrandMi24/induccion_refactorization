# Seguridad

Revisión hecha sobre el estado actual del código. Se divide en lo que ya está resuelto, y lo que queda pendiente antes de considerar la app lista para producción con usuarios reales fuera de un entorno controlado.

## Resuelto en esta revisión

- **Contraseñas en texto plano** — era el hallazgo más grave: el login comparaba `Usuarios.Contrasena` directo contra lo escrito por el usuario, sin ningún hash. Se agregó `Helpers/PasswordHasher.cs` (PBKDF2-SHA256, 100,000 iteraciones, salt aleatorio de 16 bytes, sin dependencias externas). El login ahora:
  - Si la contraseña guardada ya está hasheada (`PBKDF2$...`), verifica contra el hash.
  - Si todavía es texto plano (usuarios existentes de antes de este cambio), la compara directo y, si es correcta, la reemplaza por su versión hasheada en ese mismo momento — no se requiere una migración de datos aparte, cada usuario se actualiza solo la próxima vez que inicia sesión.
  - `CreateUsuario`, `EditUsuario` (Admin) y el cambio de contraseña en `Perfil/Edit` ahora hashean antes de guardar.
- **Longitud mínima de contraseña** — se agregó `MinimumLength = 8` a `Usuario.Contrasena` (aplica al crear un usuario nuevo).
- **Encabezados de seguridad HTTP** — se agregaron en `Web.config`: `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy: strict-origin-when-cross-origin`.

## Ya estaba bien (verificado, sin cambios necesarios)

- **CSRF**: los 22 `[HttpPost]` del proyecto tienen su `[ValidateAntiForgeryToken]` correspondiente, sin excepciones.
- **XSS**: no hay ningún uso de `Html.Raw` en las vistas; Razor escapa por defecto todo lo que se imprime con `@`.
- **Inyección SQL**: no hay SQL crudo (`SqlQuery`/`ExecuteSqlCommand`) en ningún controlador; todo pasa por LINQ-to-Entities, que parametriza automáticamente.
- **Autorización por rol**: los cuatro controladores de rol (Admin, Director, Coordinador, Aspirante) están decorados con `[RoleAuthorize(n)]`; `Perfil` con `[Authorize]`. No hay ningún controlador de datos sensibles sin protección.
- **IDOR en descargas**: `Aspirante/DownloadSubmission` valida que la submisión pertenezca al aspirante de la sesión antes de servir el archivo.
- **Archivos subidos fuera de alcance HTTP**: se guardan bajo `App_Data/Uploads/...`, carpeta que ASP.NET protege de acceso directo por URL.
- **Validación de subida de archivos**: extensión (`.pdf .docx .xlsx .jpg .jpeg .png`) y tamaño máximo (10 MB) validados en el servidor (`FileUploadValidator`), no solo en el cliente.

## Pendiente antes de producción

Estos requieren una decisión explícita tuya (cambian el comportamiento actual de desarrollo) o son trabajo adicional, así que se dejan documentados en vez de aplicarse solos:

1. **`customErrors mode="Off"` + `debug="true"` en `Web.config`** — hoy cualquier error muestra el stack trace completo (rutas de archivo del servidor, nombres de tablas, etc.) a quien sea que esté viendo la página. Esto es lo que nos ha permitido depurar los errores de compilación de Razor durante el desarrollo, pero **debe apagarse antes de exponer la app a usuarios reales**: `customErrors mode="RemoteOnly"` (o `On`) y `debug="false"`.
2. **`requireSSL="false"` en la cookie de Forms Authentication** — para producción, una vez que haya HTTPS real, cambiar a `requireSSL="true"` para que la cookie de sesión nunca viaje por HTTP.
3. **Sin bloqueo por intentos fallidos de login** — no hay límite de intentos ni lockout temporal, así que un ataque de fuerza bruta contra un usuario no está mitigado. Requiere agregar contador de intentos + ventana de bloqueo (columnas nuevas en `Usuarios` o una tabla aparte). No implementado en esta pasada por ser una funcionalidad nueva, no un fix — avisa si quieres que se agregue.
4. **Mensajes de error con detalle técnico expuestos al usuario** — varios `catch` en los controladores hacen `TempData["Error"] = $"Error: {ex.Message}"`, lo cual filtra detalles internos. Antes de producción, lo ideal es loguear el detalle en servidor (hoy no hay ningún sistema de logging configurado) y mostrar al usuario un mensaje genérico.
5. **Validación de contenido de archivos subidos** — hoy se valida por extensión y tamaño, no por contenido real (magic bytes). El riesgo está limitado porque los archivos no son servidos directamente por HTTP, pero como defensa adicional se podría verificar la firma del archivo además de la extensión.
6. **Fijación de sesión (session fixation)** — el `SessionID` de ASP.NET no se regenera al iniciar sesión. Bajo `InProc` con cookie `HttpOnly`, el riesgo práctico es bajo, pero es una mejora conocida si se quiere ir más allá del estándar.

Ninguno de estos puntos pendientes es explotable trivialmente desde fuera en el entorno de desarrollo actual (LAN/localhost); todos son ajustes esperables al pasar de "app en desarrollo" a "app en producción con usuarios reales".
