using KingNetwork.Shared;
using System;
using System.Net.Sockets;

namespace KingNetwork.Server
{
    /// <summary>
    /// This class is responsible for represents the tcp client connection.
    /// </summary>
    public class TcpClient : BaseClient
    {
        #region constructors

        /// <summary>
        /// Creates a new instance of a <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="id">The identifier number of connected client.</param>
        /// <param name="socketClient">The tcp client from connected client.</param>
        /// <param name="messageReceivedHandler">The callback of message received handler implementation.</param>
        /// <param name="clientDisconnectedHandler">The callback of client disconnected handler implementation.</param>
        /// <param name="maxMessageBuffer">The max length of message buffer.</param>
        public TcpClient(ushort id, Socket socketClient, MessageReceivedHandler messageReceivedHandler, ClientDisconnectedHandler clientDisconnectedHandler, ushort maxMessageBuffer)
        {
            try
            {
                _socketClient = socketClient;
                _messageReceivedHandler = messageReceivedHandler;
                _clientDisconnectedHandler = clientDisconnectedHandler;

                _socketClient.ReceiveBufferSize = maxMessageBuffer;
                _socketClient.SendBufferSize = maxMessageBuffer;
                _buffer = new byte[maxMessageBuffer];
                _stream = new NetworkStream(_socketClient);

                Id = id;

                _stream.BeginRead(_buffer, 0, _socketClient.ReceiveBufferSize, ReceiveDataCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        #endregion

        #region public methods implementation

        /// <summary>
        /// Method responsible for send message to client.
        /// </summary>
        /// <param name="kingBuffer">The king buffer of received message.</param>
        public override void SendMessage(KingBufferWriter kingBuffer)
        {
            try
            {
                if (IsConnected)
                {
                    _stream.Write(kingBuffer.BufferData, 0, kingBuffer.Length);
                    _stream.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }
        
        #endregion

        #region private methods implementation
        
        /// <summary> 	
        /// The callback from received message from connected client. 	
        /// </summary> 	
        /// <param name="asyncResult">The async result from a received message from connected client.</param>
        private void ReceiveDataCallback(IAsyncResult asyncResult)
        {
            try
            {
                if (_socketClient.Connected)
                {
                    var endRead = _stream.EndRead(asyncResult);

                    var numArray = new byte[endRead];

                    if (endRead != 0)
                    {
                        Buffer.BlockCopy(_buffer, 0, numArray, 0, endRead);

                        _stream.BeginRead(_buffer, 0, _socketClient.ReceiveBufferSize, ReceiveDataCallback, null);
                        
                        var buffer = KingBufferReader.Create(numArray, 0, numArray.Length);

                        _messageReceivedHandler(this, buffer);

                        return;
                    }
                }

                _socketClient.Close();
                _clientDisconnectedHandler(this);
            }
            catch (Exception ex)
            {
                _socketClient.Close();
                _clientDisconnectedHandler(this);
            }
            
            Console.WriteLine($"Client '{IpAddress}' Disconnected.");
        }

        #endregion
    }
}
