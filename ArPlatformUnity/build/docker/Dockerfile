FROM unitymultiplay/linux-base-image:1.0.1

# Install dependencies.
USER root
#RUN apt-get update 
USER mpukgame

# Set up a working directory.
WORKDIR /game

# Add your game files and perform any required init steps.
COPY ./dedicatedServer .
# RUN ./init.sh
EXPOSE 7777

# Set your game entrypoint.
ENTRYPOINT [ "/game/server.x86_64" ]