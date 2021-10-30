# BugyBot
Simple discord music bot that works with [youtube-explode](https://github.com/Tyrrrz/YoutubeExplode) and ffmpeg. Written in .NET 5

Unstable and poorly written for personal use on a small server

# Features
* Support for easy localization
* Playing Music from YouTube URL
* Playing Playlists from YouTube URL
* Queue
* Searching youtube videos from user request
* Skipping
* Volume and speed settings (in the form that allows discord and ffmpeg)
* Reverse (takes a long time to run at the moment)
* Bugs
* Restart command in case something breaks :)

# Commands
You can change prefix in config.yml
* !play [Youtube URL] - starts playing audio from youtube url
* !play [Search string] - starts playing audio from first video in youtube search
* !playlist [Youtube URL] - adds all video from playlist to queue
* !queue - queue
* !volume [from 0 to 10] - default is 1. Not sure how it works with discord
* !speed [from 0.5 to 2] - default is 1. 
* !loop - loop
* !reverse - playing audio in reverse. Works really slow
* !skip - skipping current track
* !restart - restart program. Not working with linux screen

# Installing
### Windows
1. Download
2. Paste you bot token to config.yml
3. Start and use

### Linux
1. Install libopus, libsodium and ffmpeg
```
sudo apt install libsodium-dev libopus-dev ffmpeg -y
```
2. Download
3. Paste you bot token to config.yml
4. Now you can start the bot with screen, but then the !restart command will shutdown the bot without restarting.
To avoid this, bot must be started as service.

##### Service example
/etc/systemd/system/bb.service
```
[Unit]
Description=Bugy Bot
After=network.target
StartLimitIntervalSec=0

[Service]
Type=simple
Restart=always
RestartSec=1
User=USERNAME
WorkingDirectory=/home/penguin/bcm
ExecStart=/home/penguin/bcm/BugyBot

[Install]
WantedBy=multi-user.target
```
Do not forget to paste the username from which the bot will be started. It must have access to execute the program 

Starting bot service
```
sudo systemctl bb start
```
Enable autostart with system
```
sudo systemctl bb enable
```
