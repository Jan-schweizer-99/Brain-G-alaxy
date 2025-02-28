# VR-Lernanwendung mit KI-gestützter Generierung

![Platzhalter für das Logo der Anwendung](./!github%20Content/Anwendung.PNG)

Dieses Projekt ist eine KI-gestützte VR-Lernanwendung, die automatisch eine prozedurale Galaxie mit schwebenden Inseln generiert. Die Inhalte der Inseln werden mithilfe einer lokal laufenden KI erstellt. Das System kombiniert Virtual Reality, künstliche Intelligenz und ein Video-on-Demand-Feature zu einer interaktiven Lernumgebung.

---

## Inhaltsverzeichnis
1. [Features](#features)
2. [Installation & Nutzung](#installation--nutzung)
3. [Screenshots & weitere Bilder](#screenshots--weitere-bilder)
4. [Bekannte Probleme & Workarounds](#bekannte-probleme--workarounds)
5. [Entwickler](#entwickler)
6. [Lizenz](#lizenz)

---

## Features
- **KI-gestützte Generierung**: Die Lerninhalte werden mithilfe eines lokal laufenden KI-Modells erzeugt.
- **Prozedurale VR-Umgebung**: Eine dynamische Galaxie mit schwebenden Inseln.
- **Video-on-Demand**: JSON-basierte Videobibliothek mit YouTube-API-Anbindung.
- **Unity-Integration**: Ein eigenständiges Framework zur schnellen Anpassung und Erweiterung.

## Installation & Nutzung

### 1. Benötigte Programme und Abhängigkeiten

- **Node.js** (optional für das Debugging-Tool): [https://nodejs.org/en](https://nodejs.org/en)
- **Python** (zur Erzeugung von YouTube-JSON und ggf. zusätzlicher Skripte): [https://www.python.org](https://www.python.org)
- **Ollama** (zum Ausführen der KI-Modelle lokal): [https://ollama.com/download/windows](https://ollama.com/download/windows)
- **Deepseek Models** (lokale KI-Modelle): [https://ollama.com/library/deepseek-r1](https://ollama.com/library/deepseek-r1)

### 2. Python einrichten
1. **Python und pip aktualisieren (empfohlen):**
   ```bash
   python -m ensurepip --default-pip
   python -m pip install --upgrade pip
   ```
2. **YouTube-API-Abhängigkeit:**
   ```bash
   pip install google-api-python-client
   ```
   > *Damit können Sie `YoutubeAPI.py` nutzen, um eine JSON-Datei mit YouTube-Daten zu generieren.*
3. **Optionale OAuth-Authentifizierung:**
   ```bash
   pip install google-auth google-auth-oauthlib google-auth-httplib2
   ```
   > *Nur erforderlich, wenn Sie OAuth-geschützte Daten abrufen möchten.*

### 3. Ollama & Deepseek-Modell installieren und starten
1. **Ollama herunterladen:**
   - [Ollama Windows Installer](https://ollama.com/download/windows)
2. **Deepseek-Modell verwenden:**
   - Laden Sie das gewünschte Modell, z. B. `deepseek-r1:8b` oder `deepseek-r1:14b`. Meistens reicht:
     ```bash
     ollama run deepseek-r1:8b
     ```
     oder
     ```bash
     ollama run deepseek-r1:14b
     ```
   > *Weitere Installationshinweise finden Sie im [Video ab Minute 14:55](https://youtu.be/3chfe8Q9rtQ?si=gsZIRdGRRPtP03bN&t=891).*
3. **Server-Batchdatei starten:**
   - Im Projekt unter `Assets\_scopehit\scripts\AI\start-ollama.bat` finden Sie eine Batchdatei, um Ollama lokal zu starten.

### 4. Unity-Projekt öffnen
1. **Projekt klonen oder entpacken:**
   ```bash
   git clone <repository-url>
   cd <project-folder>
   ```
2. **Unity starten:**
   - Öffnen Sie das Projekt in **Unity** (mindestens Version 2021.x oder höher, VR-Unterstützung erforderlich).
3. **Abhängigkeiten im Unity Package Manager prüfen:**
   - Stellen Sie sicher, dass alle erforderlichen Pakete (z. B. XR Interaction Toolkit) installiert sind.

### 5. Wichtige Skripte & Dateien
- **YouTube Library Skript:** `Assets\_scopehit\scripts\YoutubeLibrary\YoutubeAPI.py`
  - Erzeugt aus der YouTube-API eine JSON-Datei mit Thumbnails, Titeln etc.
- **SVG-Konverter:** `Assets\Editor\Backgroundlogos\svg-converter-web.html`
  - Wandelt SVG-Dateien in PNG-Bilder um, um Icons anzupassen.
- **Ollama Integration:** `Assets\_scopehit\scripts\AI\OllamaClient.cs` (Client-Logik)
- **Node.js WebSocket:** `Assets\_scopehit\scripts\nodejs_debug` (Debugging-Tool)

---

## Screenshots & weitere Bilder

### Anwendung.png
![Anwendung](./!github%20Content/Anwendung.PNG)

### Banner "Bild4.png"
![Banner](./!github%20Content/Bild4.png)

### Hochschullogo
![Hochschule Furtwangen Logo](./!github%20Content/Hochschule_Furtwangen_HFU_logo.png)

---

## Bekannte Probleme & Workarounds

### Scriptable Object wird beim ersten Öffnen zurückgesetzt
Beim ersten Start des Projekts kann es vorkommen, dass das **CustomBaseEditor Scriptable Object** zurückgesetzt wird.

**Workaround**: Speichern Sie das Scriptable Object einmal manuell neu.

---

## Entwickler
- **Autor:** Jan Schweizer
- **Erstbetreuer:** Prof. Dr. Stephanie Heintz
- **Zweitbetreuer:** Prof. Dr. Ruxandra Lasowski
- **Hochschule:** Hochschule Furtwangen
- **Bachelorarbeit:** Entwicklung eines Unity-Frameworks zur KI-gestützten Generierung modularer VR-Lernumgebungen

## Lizenz
Dieses Projekt steht unter einer individuellen Lizenz für akademische Zwecke. Eine kommerzielle Nutzung ist nicht vorgesehen.
