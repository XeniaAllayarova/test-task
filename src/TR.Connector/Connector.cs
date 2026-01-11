using TR.Connector.Clients;
using TR.Connector.Constants;
using TR.Connector.Extensions;
using TR.Connector.Models.DTOs;
using TR.Connector.Models.Entites;
using TR.Connector.Models.Responses;
using TR.Connector.Services;
using TR.Connectors.Api.Entities;
using TR.Connectors.Api.Interfaces;

namespace TR.Connector
{
    public partial class Connector : IConnector
    {
        public ILogger Logger { get; set; }

        private ApiClient _apiClient;

        //Пустой конструктор
        public Connector() { }

        public void StartUp(string connectionString)
        {
            string url = "", login = "", password = "";

            Logger.Debug("Строка подключения: " + connectionString);

            foreach (var item in connectionString.Split(';'))
            {
                if (item.StartsWith("url")) url = item.Split('=')[1];
                if (item.StartsWith("login")) login = item.Split('=')[1];
                if (item.StartsWith("password")) password = item.Split('=')[1];
            }

            _apiClient = new ApiClient(url, new JsonSerializerService());

            try
            {
                var tokenResponse = _apiClient.Post<TokenResponse>("api/v1/login", new { login, password });

                _apiClient.SetAccessToken(tokenResponse.data.access_token);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }

        }

        public IEnumerable<Permission> GetAllPermissions()
        {
            try
            {
                var itRoleResponse = _apiClient.Get<RoleResponse>("api/v1/roles/all");
                var itRolePermissions =
                    itRoleResponse.data.Select(_ => new Permission($"{Role.It},{_.id}", _.name, _.corporatePhoneNumber));
                var rightResponse = _apiClient.Get<RoleResponse>("api/v1/rights/all");
                var rightPermissions = rightResponse.data.Select(_ =>
                    new Permission($"{Right.Request},{_.id}", _.name, _.corporatePhoneNumber));

                return itRolePermissions.Concat(rightPermissions);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);

                return Enumerable.Empty<Permission>();
            }

        }

        public IEnumerable<string> GetUserPermissions(string userLogin)
        {
            try
            {
                var itRoleResponse = _apiClient.Get<RoleResponse>($"api/v1/users/{userLogin}/roles");
                var itRoleList = itRoleResponse.data.Select(_ => $"{Role.It},{_.id}").ToList();
                var rightResponse = _apiClient.Get<RoleResponse>($"api/v1/users/{userLogin}/rights");
                var requestRightList = rightResponse.data.Select(_ => $"{Right.Request},{_.id}").ToList();

                return itRoleList.Concat(requestRightList);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);

                return Enumerable.Empty<string>();
            }
        }

        public void AddUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            try
            {
                if (IsUserLocked(userLogin))
                {
                    Logger.Error($"Пользователь {userLogin} залочен.");

                    return;
                }

                foreach (var rightId in rightIds)
                {
                    GetPropertyInfo(rightId, out string propretyName, out string propretyId);

                    switch (propretyName)
                    {
                        case Role.It:
                            _apiClient.Put($"api/v1/users/{userLogin}/add/role/{propretyId}");

                            break;
                        case Right.Request:
                            _apiClient.Put($"api/v1/users/{userLogin}/add/right/{propretyId}");

                            break;
                        default:
                            throw new Exception($"Тип доступа {propretyName} не определен");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            try
            {
                if (IsUserLocked(userLogin))
                {
                    Logger.Error($"Пользователь {userLogin} залочен.");

                    return;
                }

                foreach (var rightId in rightIds)
                {
                    GetPropertyInfo(rightId, out string propretyName, out string propretyId);

                    switch (propretyName)
                    {
                        case Role.It:
                            _apiClient.Delete($"api/v1/users/{userLogin}/drop/role/{propretyId}");

                            break;
                        case Right.Request:
                            _apiClient.Delete($"api/v1/users/{userLogin}/drop/right/{propretyId}");

                            break;
                        default:
                            throw new Exception($"Тип доступа {propretyName} не определен");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }

        }

        public IEnumerable<Property> GetAllProperties()
        {
            var props = new List<Property>();

            foreach (var propertyInfo in new UserPropertyData().GetType().GetProperties())
            {
                if (propertyInfo.Name == PropertyConst.Login)
                {
                    continue;
                }

                props.Add(new Property(propertyInfo.Name, propertyInfo.Name));
            }
            return props;
        }

        public IEnumerable<UserProperty> GetUserProperties(string userLogin)
        {
            var user = GetUserPropertyData(userLogin);

            return user.GetType().GetProperties()
                .Select(_ => new UserProperty(_.Name, _.GetValue(user) as string));
        }

        public void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin)
        {
            try
            {
                var user = GetUserPropertyData(userLogin);

                foreach (var property in properties)
                {
                    foreach (var userProp in user.GetType().GetProperties())
                    {
                        if (property.Name == userProp.Name)
                        {
                            userProp.SetValue(user, property.Value);
                        }
                    }
                }

                _apiClient.Put("api/v1/users/edit", user);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public bool IsUserExists(string userLogin)
        {
            try
            {
                var userResponse = _apiClient.Get<UserResponse>("api/v1/users/all");
                var user = userResponse.data.FirstOrDefault(_ => _.login == userLogin);

                if (user != null)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);

                return false;
            }

        }

        public void CreateUser(UserToCreate user)
        {
            try
            {
                var newUser = new CreateUserDTO()
                {
                    login = user.Login,
                    password = user.HashPassword,
                    lastName = user.Properties.GetPropertyValue(PropertyConst.LastName),
                    firstName = user.Properties.GetPropertyValue(PropertyConst.FirstName),
                    middleName = user.Properties.GetPropertyValue(PropertyConst.MiddleName),
                    telephoneNumber = user.Properties.GetPropertyValue(PropertyConst.TelephoneNumber),
                    isLead = user.Properties.GetBoolPropertyValue(PropertyConst.IsLead),
                    status = string.Empty
                };

                _apiClient.Put("api/v1/users/create", newUser);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }

        }

        private bool IsUserLocked(string userLogin)
        {
            var userResponse = _apiClient.Get<UserResponse>("api/v1/users/all");
            var user = userResponse.data?.FirstOrDefault(_ => _.login == userLogin);

            return user != null && user.status == UserStatus.Lock;
        }

        private UserPropertyData GetUserPropertyData(string userLogin)
        {
            var userResponse = _apiClient.Get<UserPropertyResponse>($"api/v1/users/{userLogin}");
            var user = userResponse.data ?? throw new NullReferenceException($"Пользователь {userLogin} не найден");

            if (user.status == UserStatus.Lock)
            {
                throw new Exception($"Невозможно обновить свойства, пользователь {userLogin} залочен");
            }

            return user;
        }

        private static void GetPropertyInfo(string rightId, out string propretyName, out string propretyId)
        {
            var rightStr = rightId.Split(',');
            propretyName = rightStr[0];
            propretyId = rightStr[1];
        }
    }
}
