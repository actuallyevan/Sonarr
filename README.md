# Sonarr

## Installation

This fork is designed to be a drop-in replacement for existing Sonarr docker installations. Simply replace your Sonarr docker image with `ghcr.io/actuallyevan/sonarr:latest`

Sample docker compose:
```docker
sonarr:
    image: ghcr.io/actuallyevan/sonarr:latest
    container_name: sonarr
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Etc/UTC
      # Add env vars for any tweaks you want to enable
      - ACCEPT_RELEASE_ANY_UPGRADABLE=true
      - IGNORE_MATCH_BY_ID_WARNING=true
      - FIX_ANIME_SEASON_SEARCH=true
      - IMPROVE_QUEUE_RESPONSIVENESS=true
      - DISABLE_MEDIA_COVER_CACHE=true
    volumes:
      - /path/to/sonarr/data:/config
      # Additional volume mounts for your media, etc
    ports:
      - 8989:8989
    restart: unless-stopped
```

## Why this fork?

This fork aims to improve certain aspects of Sonarr to make it work better with remote "infinite" library setups (Debrid/Usenet streaming, etc). This fork will be kept up-to-date with Sonarr stable releases and you should be able to swap back and forth between them if needed.

## Fixes
These are universal bug-fixes that should be fixed in the original Sonarr project. These might make it into a general release once the v5 migration is complete.

- ffprobe issues: There's a bug in VideoFileInfoReader that causes ffprobe to read the entire file during HDR analysis if the video stream is at a non-zero index. This is especially problematic for remote files since it uses bandwidth unnecessarily.
- RefreshMonitoredDownloadsCommand: Debrid/Usenet mounting tools commonly issue this command after processing a download. But when this command is issued through the API or through the UI, it has a `Normal` priority, causing a buildup of queue items if lots of searches are triggered at once.
- Fix a bug where Sonarr silently ignores torrents that have been previously imported, deleted and then grabbed again. Without this, repairs from Debrid/Usenet mounting tools don't work reliably.

## Tweaks
These are small changes to Sonarr's behavior to optimize for Debrid/Usenet streaming setups. These can be enabled through environment variables.

#### ACCEPT_RELEASE_ANY_UPGRADABLE

This setting allows Sonarr to download season packs even if it already has some episodes. Generally you should turn this on for Debrid setups.

Sonarr does not download season packs if it does not result in an upgrade for ALL episodes in a season. This is a good choice for regular setups where downloads take a long time and downloading a season pack just to upgrade a single episode is not pragmatic. But with "infinite" setups, downloads take seconds. Furthermore, Debrid services often expire certain files from a season pack and by default, Sonarr doesn't download another season pack to replace it, which can result in Sonarr grabbing a lower quality single episode.

#### IGNORE_MATCH_BY_ID_WARNING

This setting turns off the warning:

```
Found matching series via grab history, but release was matched to series by ID. Automatic import is not possible
``` 

This generally happens on indexers that include a tvdbId in releases that Sonarr uses to match against series while downloading. But during imports, if the files are obfuscated or the file/series name doesn't match up with Sonarr's expectations, it blocks automatic import.
 
⚠️ Only use this setting if you trust your indexers to provide the correct tvdbIds when they're present on releases.

#### FIX_ANIME_SEASON_SEARCH

Sonarr's default anime season search is **VERY** slow since it also searches for each episode individually. This setting allows you to bypass that and just search by season, which most indexers support. This significantly improves the search experience for anime.

#### IMPROVE_QUEUE_RESPONSIVENESS

Ensures that the `RefreshMonitoredDownloadsCommand` and `ProcessMonitoredDownloads` commands always execute immediately by reserving 3 additional slots for these commands. This improves UI responsiveness for the current state of the activity queue, ensuring the activity queue reflects what's happening in real time, even when many search tasks are queued.

#### DISABLE_MEDIA_COVER_CACHE

When set to `true`, Sonarr will not download or store media cover images locally in the `MediaCover/` folder. Instead, cover URLs will point directly to remote sources (e.g., TVDB/TMDB). This saves disk space and I/O for infinite/remote library setups where covers are only needed for display. Existing cached images are left untouched — delete the `MediaCover/` folder manually to reclaim space.

## Contributing

Feel free to open issues or pull requests for any changes you'd like to see.