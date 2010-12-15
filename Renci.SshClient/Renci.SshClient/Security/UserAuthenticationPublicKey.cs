﻿using System;
using System.Threading;
using Renci.SshClient.Common;
using Renci.SshClient.Messages;
using Renci.SshClient.Messages.Authentication;

namespace Renci.SshClient.Security
{
    internal class UserAuthenticationPublicKey : UserAuthentication, IDisposable
    {
        private EventWaitHandle _authenticationCompleted = new AutoResetEvent(false);

        public override string Name
        {
            get
            {
                return "publickey";
            }
        }

        protected override bool Run()
        {
            if (this.Session.ConnectionInfo.KeyFile == null)
                return false;

            this.Session.RegisterMessageType<InformationRequestMessage>(MessageTypes.UserAuthenticationInformationRequest);

            //  TODO:   Complete full public key implementation which includes other messages
            var message = new PublicKeyRequestMessage
            {
                ServiceName = ServiceNames.Connection,
                Username = this.Session.ConnectionInfo.Username,
                PublicKeyAlgorithmName = this.Session.ConnectionInfo.KeyFile.AlgorithmName,
                PublicKeyData = this.Session.ConnectionInfo.KeyFile.PublicKey,
                //Signature = new byte[] { },
            };

            var signatureData = new SignatureData(message, this.Session.SessionId.GetSshString()).GetBytes();

            message.Signature = this.Session.ConnectionInfo.KeyFile.GetSignature(signatureData);

            this.Session.SendMessage(message);

            this.Session.WaitHandle(this._authenticationCompleted);

            this.Session.UnRegisterMessageType(MessageTypes.UserAuthenticationInformationRequest);


            return true;
        }

        protected override void Session_UserAuthenticationSuccessMessageReceived(object sender, MessageEventArgs<SuccessMessage> e)
        {
            base.Session_UserAuthenticationSuccessMessageReceived(sender, e);
            this._authenticationCompleted.Set();
        }

        protected override void Session_UserAuthenticationFailureReceived(object sender, MessageEventArgs<FailureMessage> e)
        {
            base.Session_UserAuthenticationFailureReceived(sender, e);
            this._authenticationCompleted.Set();
        }

        private class SignatureData : SshData
        {

            private PublicKeyRequestMessage _message;

            private string _sessionId;

            public SignatureData(PublicKeyRequestMessage message, string sessionId)
            {
                this._message = message;
                this._sessionId = sessionId;
            }

            protected override void LoadData()
            {
                throw new System.NotImplementedException();
            }

            protected override void SaveData()
            {
                this.Write(this._sessionId);
                this.Write((byte)this._message.MessageType);
                this.Write(this._message.Username);
                this.Write("ssh-connection");
                this.Write("publickey");
                this.Write((byte)1);
                this.Write(this._message.PublicKeyAlgorithmName);
                this.Write(this._message.PublicKeyData.GetSshString());
            }
        }

        #region IDisposable Members

        private bool _isDisposed = false;

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._isDisposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    if (this._authenticationCompleted != null)
                    {
                        this._authenticationCompleted.Dispose();
                    }
                }

                // Note disposing has been done.
                _isDisposed = true;
            }
        }

        ~UserAuthenticationPublicKey()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion
    }
}
