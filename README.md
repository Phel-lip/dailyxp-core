
# DailyXP Core

Aplicação Full Stack de gerenciamento de hábitos, com sistema simples de recompensas por XP.

O projeto permite cadastrar, visualizar, concluir, reabrir e excluir hábitos. Cada hábito concluído vale 20 XP.

## Tecnologias

- React + Vite
- JavaScript, HTML e CSS
- C# / ASP.NET Core (.NET 10)
- PostgreSQL (Neon)
- Npgsql
- API REST

## Funcionalidades

- Cadastro e listagem de hábitos
- Conclusão e reabertura de tarefas
- Exclusão de hábitos
- Contagem de hábitos concluídos e XP
- Persistência de dados no PostgreSQL
- Separação de hábitos por navegador
- Interface responsiva
- Limitação básica de requisições na API

## Arquitetura

React → Requisições HTTP → API C# → PostgreSQL

O frontend consome os endpoints REST desenvolvidos em ASP.NET Core.

A API utiliza Npgsql para executar comandos SQL parametrizados no PostgreSQL.

Cada navegador possui um identificador anônimo, armazenado no localStorage. A API utiliza esse identificador para filtrar os hábitos.

Essa separação é destinada a uma demonstração com dados fictícios e não substitui autenticação de usuários.

## Estrutura

- `frontend/` — interface React
- `backend/` — API ASP.NET Core
- `Dockerfile` — configuração para hospedagem da API

## Desenvolvimento

O DailyXP Core foi desenvolvido para consolidar, em uma aplicação mais enxuta, os conhecimentos adquiridos durante meus estudos de desenvolvimento Full Stack.

O projeto prioriza uma arquitetura simples, CRUD, integração entre frontend e backend e persistência em banco de dados relacional.

## Projeto original

Esta versão é uma consolidação do [DailyXP original](https://github.com/Phel-lip/dailyxp), que possui uma arquitetura mais completa, incluindo autenticação JWT.

O DailyXP Core mantém o conceito de gamificação de hábitos, com foco em funcionalidades essenciais e em uma implementação mais fácil de compreender e manter.
