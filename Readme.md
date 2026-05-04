# Document Manager

Система управління документами з підтримкою папок, версіонуванням файлів, історією змін та повним тестовим покриттям (модульні, інтеграційні, тести БД, навантажувальні). Проєкт реалізовано на **.NET 8**, **PostgreSQL**, **Entity Framework Core** з використанням **Clean Architecture**.

## 🚀 Основні можливості

- Ієрархічна структура папок (максимальна глибина – 10 рівнів)
- Збереження документів у папках (унікальність назви в межах папки)
- **Версіонування документів** – кожне оновлення створює нову версію
- Історія версій із зазначенням автора та змісту змін
- Перевірка бізнес-правил:
  - унікальність назви папки в межах батьківської
  - унікальність назви документа в межах папки
  - заборона видалення непорожньої папки
  - обмеження розміру файлу 50 МБ
  - обмеження глибини вкладеності папок (10)
- Пошук документів за назвою
- REST API з повною документацією Swagger

## 🛠 Стек технологій

- **.NET 8** (LTS)
- **PostgreSQL** (база даних)
- **Entity Framework Core** (ORM, міграції)
- **xUnit, FluentAssertions, Moq** (модульні тести)
- **Testcontainers** (інтеграційні тести з реальною PostgreSQL)
- **k6** (навантажувальне тестування)
- **GitHub Actions** (CI/CD – автоматичний запуск тестів)
- **Docker, docker-compose** (контейнеризація)

## 🧱 Архітектура

Проєкт побудований за принципами **Clean Architecture**:

- `DocumentManager.Core` – доменні сутності, інтерфейси, винятки.
- `DocumentManager.Infrastructure` – реалізація репозиторіїв (EF Core), міграції, сідер даних.
- `DocumentManager.API` – контролери, DTO, глобальна обробка помилок, Swagger.
- Тестові проєкти:
  - `UnitTests` – модульні тести сервісів
  - `IntegrationTests` – тестування API через WebApplicationFactory
  - `DatabaseTests` – тести з реальною PostgreSQL (Testcontainers)
  - `PerformanceTests` – сценарії навантаження (k6)

## ⚙️ Запуск проєкту

### 1. Клонування репозиторію

``bash
git clone https://github.com/ValerijSemenuk/DocumentManager.git
cd DocumentManager
2. Налаштування бази даних
Варіант А: За допомогою Docker Compose (рекомендовано)
bash

docker-compose up -d

Це підніме PostgreSQL на порту 5432 з базою docmanager, логін postgres, пароль postgres.
Варіант Б: Локальний PostgreSQL

Створіть базу даних docmanager та оновіть рядок підключення в DocumentManager.API/appsettings.json:
json

"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=docmanager;Username=postgres;Password=ваш_пароль"
}

3. Застосування міграцій та наповнення бази тестовими даними

При першому запуску API автоматично:

    створить схему БД (якщо вона ще не створена)

    згенерує понад 20 000 записів (папки, документи, версії) за допомогою Bogus.

4. Запуск API
bash

dotnet run --project DocumentManager.API

Swagger буде доступний за адресою: http://localhost:5160/swagger
📚 API Ендпоінти
Метод	Маршрут	Опис
GET	/api/folders	Отримати всі кореневі папки
GET	/api/folders/{id}	Отримати вміст папки (підпапки + документи)
POST	/api/folders	Створити нову папку
DELETE	/api/folders/{id}	Видалити папку (тільки якщо порожня)
POST	/api/documents	Завантажити новий документ
GET	/api/documents/{id}	Отримати метадані документа
PUT	/api/documents/{id}	Оновити документ (створює нову версію)
DELETE	/api/documents/{id}	Видалити документ
GET	/api/documents/{id}/versions	Отримати історію версій
GET	/api/documents/search?name={query}	Пошук документів за назвою

    Усі відповіді повертаються у вигляді DTO (без циклічних посилань). Помилки валідації – 400 Bad Request з описом.

🧪 Тестування
Модульні тести (Unit Tests)
bash

dotnet test DocumentManager.UnitTests

Перевіряють:

    збільшення версії при оновленні

    обмеження розміру файлу (50 МБ)

    унікальність назви документа в папці

    оновлення UpdatedAt

    валідацію глибини папок (10 рівнів)

    заборону видалення непорожньої папки

Інтеграційні тести (WebApplicationFactory)
bash

dotnet test DocumentManager.IntegrationTests

Тестують повні сценарії:

    створення/оновлення/видалення документів

    навігацію по папках

    історію версій

    пошук документів

Тести бази даних (Testcontainers)
bash

dotnet test DocumentManager.DatabaseTests

Піднімають реальний PostgreSQL у Docker-контейнері та перевіряють:

    унікальність назв папок/документів

    рекурсивне завантаження дерева папок

    цілісність ланцюжка версій

Тести продуктивності (k6)
bash

cd DocumentManager.PerformanceTests
k6 run performance-test.js

Сценарій імітує одночасну роботу користувачів:

    навігація по папках

    створення документів

    оновлення (версіонування)

    отримання історії версій

🔄 CI/CD (GitHub Actions)

У репозиторії налаштовано єдиний CI pipeline (.github/workflows/ci.yml), який автоматично:

    виконує збірку проєкту

    запускає модульні, інтеграційні та тести бази даних

    використовує PostgreSQL як службу (service)

Всі workflow запускаються при push або pull request у гілки main/develop. Статус відображається на сторінці Pull Request та у вкладці Actions.
🐳 Docker

    Створення образу: docker build -t documentmanager-api .

    Запуск всього стеку: docker-compose up -d

    API стає доступним на http://localhost:5160

📝 Приклад використання (через curl)
bash

# Створення папки
curl -X POST http://localhost:5160/api/folders \
  -H "Content-Type: application/json" \
  -d '{"name":"Проєкти","parentFolderId":null,"createdBy":"admin"}'

# Створення документа
curl -X POST http://localhost:5160/api/documents \
  -H "Content-Type: application/json" \
  -d '{"folderId":"<GUID_папки>","name":"spec.pdf","contentType":"application/pdf","sizeBytes":102400}'

# Оновлення документа (нова версія)
curl -X PUT http://localhost:5160/api/documents/<GUID_документа> \
  -H "Content-Type: application/json" \
  -d '{"name":"spec_v2.pdf","sizeBytes":204800}'

# Отримання історії версій
curl http://localhost:5160/api/documents/<GUID_документа>/versions

# Пошук документів
curl "http://localhost:5160/api/documents/search?name=spec"

👥 Автор

Valerij Semenuk
