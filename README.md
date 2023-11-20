# Selfhosted AR-Platform

This project aims to provide an open source alternative for projects like Microsoft Remote Assist.

## Setup

The project utilizes docker-compose to setup the required services. To start the services, run the following command:

All necessary configuration is done via environment variables.  
1. Copy/rename the `.env.example` file to `.env` and adjust the values to your needs.
> The HOSTNAME property should follow the following format: `HOSTNAME=https://example.com`

To setup the project, run the following command in the root directory:

```bash
docker-compose up -d
```

This will create a container for the database and the backend.

With this startup Process there will be a user and a Demo Session created. You can log in with:
> Username: Administator  
> Password: Administator1!

