module CoreServer
{
    interface ServiceRegistrator
    {
        void registerChatService(Frontend.ChatService* service);
    }
}