[Fase 3](/docs/Fase%203/phase3.md)

## Build & Tests
| CI | Status |
| --- | --- | 
| Validação PR| [![Pré-validação de Release/Hotfix](https://github.com/leandrokuranaga/Fiap.Events/actions/workflows/validate-release-pr.yml/badge.svg)](https://github.com/leandrokuranaga/Fiap.Events/actions/workflows/validate-release-pr.yml)
| Validação PR Main e Dev | [![Validate PRs for Main and Dev](https://github.com/leandrokuranaga/Fiap.Events/actions/workflows/validate-pr.yml/badge.svg)](https://github.com/leandrokuranaga/Fiap.Events/actions/workflows/validate-pr.yml)
| Release | [![Release on Main (.NET 8)](https://github.com/leandrokuranaga/Fiap.Events/actions/workflows/release.yml/badge.svg)](https://github.com/leandrokuranaga/Fiap.Events/actions/workflows/release.yml)


## Deployment
| CD | Status |
| --- | --- | 
| AWS | [![Deploy to AWS ECS](https://github.com/leandrokuranaga/Fiap.Events/actions/workflows/deploy-aws.yml/badge.svg)](https://github.com/leandrokuranaga/Fiap.Events/actions/workflows/deploy-aws.yml) | 


Para executar a function é necessário usar o endpoint de criar usuário do FCG e um email chegará ao usuário criado

O FCG manda uma mensagem ao SQS e a Function ativa como trigger o SQS e executa o código