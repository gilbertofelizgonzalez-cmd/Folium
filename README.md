# Folium

Aplicación de escritorio para Windows que reúne las herramientas más habituales para trabajar con archivos PDF, todo en un único ejecutable sin instalación.

---

## Herramientas incluidas

| Herramienta | Descripción |
|---|---|
| **Unir PDFs** | Combina varios archivos PDF en uno, con miniaturas y reordenación |
| **Dividir PDF** | Extrae páginas individuales o rangos de un PDF |
| **Convertir a PDF** | Convierte imágenes (JPG, PNG, TIFF, BMP, WebP, HEIC…) y documentos Office a PDF |
| **Comprimir PDF** | Reduce el tamaño del archivo activando la compresión de flujos internos |
| **Rotar páginas** | Rota páginas de forma individual o en bloque en pasos de 90° |
| **Reorganizar páginas** | Reordena o elimina páginas con miniaturas y botones de mover |
| **Marca de agua** | Añade texto semitransparente con control de fuente, color, opacidad y ángulo |
| **PDF a imágenes** | Exporta cada página como PNG a una carpeta |
| **Cifrado** | Cifra uno o varios PDFs con contraseña; incluye generador de contraseñas y exportación a CSV |
| **Quitar contraseña** | Elimina la protección de un PDF cifrado |

Funciones transversales:
- Tema claro / oscuro / sistema (Fluent / ModernWpf)
- Carpeta de salida predefinida opcional
- Arrastrar y soltar en todas las pantallas
- Barra de progreso y cancelación en operaciones largas

---

## Tecnologías

- **.NET 10 / WPF** — framework de UI
- **ModernWpfUI 0.9.6** — estilo Fluent, soporte de tema oscuro
- **CommunityToolkit.Mvvm 8.4.2** — source generators para MVVM (`[ObservableProperty]`, `[RelayCommand]`)
- **PdfSharpCore 1.3.67** — manipulación de PDFs: unir, dividir, rotar, marcar, cifrar
- **PDFtoImage 5.1.0** — renderizado de páginas a imágenes (PDFium)
- **SkiaSharp 3.119.0** — procesamiento de miniaturas
- **Microsoft.Extensions.DependencyInjection** — inyección de dependencias

Conversión de documentos Office (opcional):
- **LibreOffice** (detección automática)
- **Syncfusion Community** (si está instalado)

---

## Requisitos

- Windows 10 versión 1809 o superior, 64-bit
- Sin instalación adicional — el ejecutable incluye el runtime de .NET

---

## Instalación

Descarga `Folium.exe` desde [Releases](../../releases) y ejecútalo directamente. No requiere instalador.

---

## Compilar desde el código fuente

**Requisito:** .NET 10 SDK — https://dot.net

```bash
git clone https://github.com/gilbertofelizgonzalez-cmd/Folium.git
cd Folium
dotnet build src/PdfToolkit.UI/PdfToolkit.UI.csproj -c Release
```

Publicar como ejecutable autónomo:

```bash
dotnet publish src/PdfToolkit.UI/PdfToolkit.UI.csproj ^
  -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -o publish/Folium
```

El archivo resultante es `publish/Folium/Folium.exe` (~72 MB, sin dependencias externas).

---

## Estructura del proyecto

```
src/
  PdfToolkit.Core/        Interfaces, modelos de solicitud/resultado y excepciones
  PdfToolkit.Services/    Implementaciones (PdfSharpCore, PDFtoImage, SkiaSharp)
  PdfToolkit.UI/          Aplicación WPF — vistas, ViewModels, servicios de UI
tests/
  PdfToolkit.Services.Tests/
docs/
  DOCUMENTACION.txt       Documentación completa de usuario y desarrollo
_icongen/
  IconGen/                Generador del icono de la aplicación (SkiaSharp)
```

---

## Licencia

MIT
