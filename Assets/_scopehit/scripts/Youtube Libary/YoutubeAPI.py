import json
import os
import re
from googleapiclient.discovery import build
from urllib.parse import urlparse, parse_qs

def extract_channel_id_from_url(url):
    """Extract channel ID or playlist ID from a YouTube URL."""
    parsed_url = urlparse(url)
    
    # Check if it's a valid YouTube URL
    if 'youtube.com' not in parsed_url.netloc and 'youtu.be' not in parsed_url.netloc:
        raise ValueError(f"❌ Ungültige YouTube-URL: {url}")
    
    # Extract channel ID from different URL formats
    path = parsed_url.path
    query = parse_qs(parsed_url.query)
    
    # Handle playlist URLs
    if '/playlist' in path and 'list' in query:
        playlist_id = query['list'][0]
        return {'type': 'playlist', 'id': playlist_id}
    
    # Handle channel URLs
    if '/channel/' in path:
        channel_id = path.split('/channel/')[1].split('/')[0]
        return {'type': 'channel', 'id': channel_id}
    
    # Handle user URLs
    if '/user/' in path:
        username = path.split('/user/')[1].split('/')[0]
        return {'type': 'user', 'name': username}
    
    # Handle c/ URLs (custom URL)
    if '/c/' in path:
        custom_name = path.split('/c/')[1].split('/')[0]
        return {'type': 'custom', 'name': custom_name}
    
    # Handle handle URLs (@username)
    if '/@' in path:
        handle = path.split('/@')[1].split('/')[0]
        return {'type': 'handle', 'name': handle}
    
    raise ValueError(f"❌ Konnte keine Kanal- oder Playlist-ID aus der URL extrahieren: {url}")

def get_channel_id_and_name(api_key, input_string):
    youtube = build('youtube', 'v3', developerKey=api_key)
    
    # Initialize variables to track if this is a playlist
    is_playlist = False
    playlist_id = None
    
    # Check if input is a URL
    if input_string.startswith('http'):
        extracted_info = extract_channel_id_from_url(input_string)
        
        if extracted_info['type'] == 'playlist':
            is_playlist = True
            playlist_id = extracted_info['id']
            
            # Get playlist details to find channel
            playlist_response = youtube.playlists().list(
                part='snippet',
                id=playlist_id
            ).execute()
            
            if not playlist_response.get('items'):
                raise ValueError(f"❌ Keine Playlist mit der ID '{playlist_id}' gefunden.")
            
            channel_id = playlist_response['items'][0]['snippet']['channelId']
            playlist_title = playlist_response['items'][0]['snippet']['title']
            channel_response = youtube.channels().list(
                part='snippet',
                id=channel_id
            ).execute()
            
            if not channel_response.get('items'):
                raise ValueError(f"❌ Kein Kanal für die Playlist '{playlist_title}' gefunden.")
            
            channel_name = channel_response['items'][0]['snippet']['title'].replace("@", "").replace(" ", "_")
            print(f"🔗 Gefundene Playlist: '{playlist_title}' vom Kanal '{channel_name}'")
            return channel_id, channel_name, is_playlist, playlist_id, playlist_title
        
        elif extracted_info['type'] == 'channel':
            channel_id = extracted_info['id']
            channel_response = youtube.channels().list(
                part='snippet',
                id=channel_id
            ).execute()
            
            if not channel_response.get('items'):
                raise ValueError(f"❌ Kein Kanal mit der ID '{channel_id}' gefunden.")
            
            channel_name = channel_response['items'][0]['snippet']['title'].replace("@", "").replace(" ", "_")
            print(f"🔗 Gefundener Kanal: '{channel_name}' mit ID '{channel_id}'")
            return channel_id, channel_name, is_playlist, None, None
        
        elif extracted_info['type'] in ['user', 'custom', 'handle']:
            search_term = extracted_info['name']
            search_response = youtube.search().list(
                part='snippet',
                q=search_term,
                type='channel',
                maxResults=1
            ).execute()
            
            if search_response.get('items'):
                channel_id = search_response['items'][0]['snippet']['channelId']
                channel_name = search_response['items'][0]['snippet']['channelTitle'].replace("@", "").replace(" ", "_")
                print(f"🔗 Gefundene Kanal-ID für '{search_term}': {channel_id}")
                return channel_id, channel_name, is_playlist, None, None
            else:
                raise ValueError(f"❌ Kein Kanal für '{search_term}' gefunden.")
    
    # Handle @handle format
    elif input_string.startswith('@'):
        handle = input_string[1:]
        search_response = youtube.search().list(
            part='snippet',
            q=handle,
            type='channel',
            maxResults=1
        ).execute()
        
        if search_response.get('items'):
            channel_id = search_response['items'][0]['snippet']['channelId']
            channel_name = search_response['items'][0]['snippet']['channelTitle'].replace("@", "").replace(" ", "_")
            print(f"🔗 Gefundene Kanal-ID für '{handle}': {channel_id}")
            return channel_id, channel_name, is_playlist, None, None
        else:
            raise ValueError(f"❌ Kein Kanal mit dem Handle '{handle}' gefunden.")
    
    # Check if input is a playlist ID
    elif input_string.startswith('PL'):
        is_playlist = True
        playlist_id = input_string
        
        # Get playlist details to find channel
        playlist_response = youtube.playlists().list(
            part='snippet',
            id=playlist_id
        ).execute()
        
        if not playlist_response.get('items'):
            raise ValueError(f"❌ Keine Playlist mit der ID '{playlist_id}' gefunden.")
        
        channel_id = playlist_response['items'][0]['snippet']['channelId']
        playlist_title = playlist_response['items'][0]['snippet']['title']
        channel_response = youtube.channels().list(
            part='snippet',
            id=channel_id
        ).execute()
        
        if not channel_response.get('items'):
            raise ValueError(f"❌ Kein Kanal für die Playlist '{playlist_title}' gefunden.")
        
        channel_name = channel_response['items'][0]['snippet']['title'].replace("@", "").replace(" ", "_")
        print(f"🔗 Gefundene Playlist: '{playlist_title}' vom Kanal '{channel_name}'")
        return channel_id, channel_name, is_playlist, playlist_id, playlist_title
    
    # Assume it's a channel ID
    else:
        channel_response = youtube.channels().list(
            part='snippet',
            id=input_string
        ).execute()
        
        if not channel_response.get('items'):
            raise ValueError(f"❌ Kein Kanal mit der ID '{input_string}' gefunden.")
        
        channel_name = channel_response['items'][0]['snippet']['title'].replace("@", "").replace(" ", "_")
        return input_string, channel_name, is_playlist, None, None

def get_channel_banner_and_avatar(api_key, channel_id):
    youtube = build('youtube', 'v3', developerKey=api_key)

    channel_response = youtube.channels().list(
        part='brandingSettings,snippet',
        id=channel_id
    ).execute()

    if not channel_response.get('items'):
        raise ValueError(f"❌ Kein Kanal mit der ID '{channel_id}' gefunden.")

    branding_settings = channel_response['items'][0]['brandingSettings']
    snippet = channel_response['items'][0]['snippet']

    # Komplette Web-URL sicherstellen
    banner_url = None
    banner_qualities = [
        'bannerExternalUrl',
        'bannerMobileExtraHdImageUrl',
        'bannerMobileHdImageUrl',
        'bannerTabletExtraHdImageUrl',
        'bannerTabletHdImageUrl'
    ]

    for quality in banner_qualities:
        banner_url = branding_settings.get('image', {}).get(quality)
        if banner_url and banner_url.startswith('//'):
            banner_url = 'https:' + banner_url
        if banner_url:
            break
            
    # Füge =w1920 zum Banner-URL hinzu, um Full-HD-Auflösung zu erhalten
    if banner_url:
        banner_url = banner_url + "=w1920"

    avatar_url = snippet.get('thumbnails', {}).get('high', {}).get('url')
    if avatar_url and avatar_url.startswith('//'):
        avatar_url = 'https:' + avatar_url

    return banner_url, avatar_url

def get_best_thumbnail(thumbnails):
    for quality in ["maxres", "standard", "high", "medium", "default"]:
        if quality in thumbnails:
            thumbnail_url = thumbnails[quality]['url']
            if thumbnail_url.startswith('//'):
                thumbnail_url = 'https:' + thumbnail_url
            return thumbnail_url
    return None

def get_channel_videos(api_key, channel_id, playlist_id=None):
    youtube = build('youtube', 'v3', developerKey=api_key)
    
    if playlist_id:
        # Wenn eine Playlist-ID angegeben wurde, lade Videos aus dieser Playlist
        videos = []
        next_page_token = None
        
        while True:
            playlist_response = youtube.playlistItems().list(
                part='snippet',
                playlistId=playlist_id,
                maxResults=50,
                pageToken=next_page_token
            ).execute()
            
            for item in playlist_response.get('items', []):
                video_title = item['snippet']['title']
                video_description = item['snippet'].get('description', '')
                video_id = item['snippet']['resourceId']['videoId']
                video_url = f"https://www.youtube.com/watch?v={video_id}"
                thumbnail_url = get_best_thumbnail(item['snippet']['thumbnails'])
                
                videos.append({
                    "id": video_id,
                    "title": video_title,
                    "description": video_description,
                    "url": video_url,
                    "thumbnail_url": thumbnail_url
                })
            
            next_page_token = playlist_response.get('nextPageToken')
            if not next_page_token:
                break
        
        return videos
    else:
        # Lade alle Videos des Kanals
        channel_response = youtube.channels().list(
            part='contentDetails',
            id=channel_id
        ).execute()
        
        uploads_playlist_id = channel_response['items'][0]['contentDetails']['relatedPlaylists']['uploads']
        
        videos = []
        next_page_token = None
        
        while True:
            playlist_response = youtube.playlistItems().list(
                part='snippet',
                playlistId=uploads_playlist_id,
                maxResults=50,
                pageToken=next_page_token
            ).execute()
            
            for item in playlist_response.get('items', []):
                video_title = item['snippet']['title']
                video_description = item['snippet'].get('description', '')
                video_id = item['snippet']['resourceId']['videoId']
                video_url = f"https://www.youtube.com/watch?v={video_id}"
                thumbnail_url = get_best_thumbnail(item['snippet']['thumbnails'])
                
                videos.append({
                    "id": video_id,
                    "title": video_title,
                    "description": video_description,
                    "url": video_url,
                    "thumbnail_url": thumbnail_url
                })
            
            next_page_token = playlist_response.get('nextPageToken')
            if not next_page_token:
                break
        
        return videos

def save_data_to_json(data, filename):
    with open(filename, "w", encoding="utf-8") as json_file:
        json.dump(data, json_file, ensure_ascii=False, indent=4)
    print(f"💾 Daten erfolgreich in '{filename}' gespeichert.")

if __name__ == "__main__":
    print("📺 YouTube Kanal-Daten Extrahierungs-Tool 📺")
    print("-------------------------------------------")
    
    # Benutzer nach API-Key fragen
    API_KEY = input("🔑 Bitte gib deinen YouTube API-Key ein: ")
    if not API_KEY:
        print("❌ API-Key erforderlich. Bitte starte das Programm neu und gib einen gültigen API-Key ein.")
        exit(1)
        
    CHANNEL_INPUT = input("📺 Gib die YouTube-Kanal-ID, Playlist-ID, Kanal-URL oder Playlist-URL ein: ")

    try:
        channel_id, channel_name, is_playlist, playlist_id, playlist_title = get_channel_id_and_name(API_KEY, CHANNEL_INPUT)
        
        if is_playlist:
            print(f"\n🔍 Lade Daten von der Playlist '{playlist_title}' vom Kanal {channel_id} ({channel_name}) ...\n")
        else:
            print(f"\n🔍 Lade Daten vom Kanal {channel_id} ({channel_name}) ...\n")

        banner_url, avatar_url = get_channel_banner_and_avatar(API_KEY, channel_id)
        videos = get_channel_videos(API_KEY, channel_id, playlist_id if is_playlist else None)

        # Speichere Kanal-URL und/oder Playlist-URL
        channel_url = f"https://www.youtube.com/channel/{channel_id}"
        playlist_url = f"https://www.youtube.com/playlist?list={playlist_id}" if playlist_id else None

        data = {
            "channel_id": channel_id,
            "channel_name": channel_name,
            "channel_url": channel_url,
            "banner_url": banner_url,
            "avatar_url": avatar_url,
            "is_playlist": is_playlist
        }
        
        if is_playlist:
            data["playlist_id"] = playlist_id
            data["playlist_title"] = playlist_title
            data["playlist_url"] = playlist_url
            
        data["videos"] = videos

        script_dir = os.path.dirname(os.path.abspath(__file__))
        
        # Dateipfad anpassen, je nachdem ob es eine Playlist ist oder nicht
        if is_playlist:
            safe_playlist_title = re.sub(r'[\\/*?:"<>|]', "", playlist_title).replace(" ", "_")
            json_filename = os.path.join(script_dir, f"{channel_name}_Playlist_{safe_playlist_title}.json")
        else:
            json_filename = os.path.join(script_dir, f"{channel_name}.json")
            
        save_data_to_json(data, json_filename)

    except ValueError as e:
        print(e)
    except Exception as e:
        print(f"❌ Ein Fehler ist aufgetreten: {str(e)}")
        print("Bitte überprüfe deinen API-Key und versuche es erneut.")