# Selfhosted AR-Platform

This project aims to provide an open-source alternative for projects like Microsoft Remote Assist.

## Setup

The project utilizes `docker-compose` to set up the required services. The startup process is as follows:

All necessary configuration is done via environment variables.

1. Copy or rename the `.env.example` file to `.env` and adjust the values to your needs.
    - The `HOSTNAME` property should follow the format: `HOSTNAME=https://example.com`

2. Download the latest release of the WebGL files and place them in a folder called `dist` within the volume referenced in the `WEBGL_VOLUME` variable. It should have the following structure: `<volumePath>/dist/index.html`

3. Download the appx package for the HoloLens and install it via the device portal. Refer to the [tutorial](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-the-windows-device-portal) for guidance.

4. Create a PFX certificate file in your certs mount volume and set credentials in the `.env` file.

5. Run the following command in the root directory:

    ```bash
    docker-compose up -d
    ```

   This will create containers for the database and the backend.

Upon completion of the startup process, a user and a demo session will be created. You can log in with the following credentials:
   - Username: Administrator
   - Password: Administrator1!

## Development

TODO

## FAQ

Due to the Unity server inside a Docker container not having access to the GPU currently, the image projection feature does not work.

- To enable this feature, manually start the GUI version of the server with the corresponding environment variables from the automatically started Docker container. These can be obtained either by using `docker inspect` or by checking the logs of the backend when the container was started.

> Ensure the Unity server container is stopped before starting the GUI server.
