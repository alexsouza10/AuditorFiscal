# Publica o Auditor Fiscal como executável único (win-x64) e empacota o resultado
# em dist/<versão>/, além de um .zip pronto para distribuir.
#
# Uso:
#   make deploy           # bump automático de minor (v0.1.0 -> v0.2.0), cria a tag E
#                          # já publica com ela — o comando padrão para mandar uma
#                          # versão nova para as pessoas, sempre gerando uma tag nova
#   make deploy 1.2.3.4   # em vez do bump automático, cria a tag "v1.2.3.4" exata e
#                          # já publica com ela (não precisa digitar o "v" na frente)
#   make            # só publica, usando a última tag do git como versão (não cria tag)
#   make release    # idem
#   make version    # só mostra qual versão seria usada
#   make tag        # só cria e publica a tag (aceita "make tag 1.2.3.4" também), sem
#                    # gerar o pacote
#   make clean      # apaga dist/
#
# A versão vem de "git describe --tags". "release"/"publish" NUNCA criam tag nova —
# eles só empacotam o que já existe. Quem cria a tag é "make tag" (chamado
# internamente por "make deploy").

# Força o Git Bash como shell das receitas, não importa de onde "make" foi chamado
# (PowerShell e cmd.exe usam cmd.exe por padrão, que não entende $$, cut, ${VAR#...} etc.
# e existe um bash.exe "stub" do WSL em system32 que também não serve aqui).
ifeq ($(OS),Windows_NT)
SHELL := C:/Program Files/Git/bin/bash.exe
.SHELLFLAGS := -c
endif

APP_PROJECT := src/AuditorFiscal.UI/AuditorFiscal.UI.csproj
RUNTIME     := win-x64
DIST_DIR    := dist

# git describe usa a última tag alcançável (ex.: "v1.2.0" ou "v1.2.0-3-gabc1234" se houver
# commits depois dela); sem nenhuma tag no repositório, cai num marcador de versão de
# desenvolvimento — "0.0.0-dev" é SemVer válido, ao contrário do hash bruto do commit.
GIT_DESCRIBE := $(shell git describe --tags 2>/dev/null)
VERSION      := $(if $(GIT_DESCRIBE),$(patsubst v%,%,$(GIT_DESCRIBE)),0.0.0-dev)
PUBLISH_DIR  := $(DIST_DIR)/$(VERSION)
ZIP_FILE     := $(DIST_DIR)/AuditorFiscal-$(VERSION)-$(RUNTIME).zip

# Permite "make deploy 1.2.3.4" ou "make tag 1.2.3.4": a palavra extra vira a versão a
# taguear em vez de o make tentar (e falhar) achar uma receita chamada "1.2.3.4". Sem
# argumento extra, os dois caem no bump automático de minor de sempre.
ifneq (,$(filter deploy tag,$(firstword $(MAKECMDGOALS))))
DEPLOY_VERSION := $(word 2,$(MAKECMDGOALS))
ifneq (,$(DEPLOY_VERSION))
$(eval $(DEPLOY_VERSION):;@:)
endif
endif
# "v" tirado aqui (função do Make, não do shell) porque DEPLOY_VERSION não existe como
# variável de ambiente dentro da receita — só como variável do Make.
DEPLOY_TAG := v$(patsubst v%,%,$(DEPLOY_VERSION))

.PHONY: all release clean version tag deploy publish

all: release
publish: release

version:
	@:; echo "$(VERSION)"

# Com versão explícita (make tag 1.2.3.4), tagueia exatamente ela. Sem argumento, faz
# bump de MINOR a partir da última tag (v0.1.0 -> v0.2.0; sem tag nenhuma, começa em
# v0.1.0). De um jeito ou de outro, cria a tag anotada no commit atual e já publica no
# remoto — nada fica só local esquecido.
tag:
	@if [ -n "$(DEPLOY_VERSION)" ]; then \
		NEW_TAG="$(DEPLOY_TAG)"; \
		if git rev-parse "$$NEW_TAG" >/dev/null 2>&1; then \
			echo "Erro: a tag $$NEW_TAG já existe." >&2; exit 1; \
		fi; \
		echo "Nova tag (versão informada): $$NEW_TAG"; \
	else \
		LAST_TAG=$$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0"); \
		VER=$${LAST_TAG#v}; \
		MAJOR=$$(echo $$VER | cut -d. -f1); \
		MINOR=$$(echo $$VER | cut -d. -f2); \
		MINOR=$$((MINOR + 1)); \
		NEW_TAG="v$$MAJOR.$$MINOR.0"; \
		echo "Última tag: $$LAST_TAG  ->  Nova tag: $$NEW_TAG"; \
	fi; \
	git tag -a "$$NEW_TAG" -m "Release $$NEW_TAG"; \
	git push origin "$$NEW_TAG"; \
	echo "Tag $$NEW_TAG criada e publicada em origin. Rode 'make release' para gerar o instalável."

# -p:RestoreConfigFile (usado em "release" abaixo) força o NuGet a usar só o
# NuGet.Config do repo, sem tentar mesclar com os configs globais da máquina/usuário.
# Em algumas instalações do Windows um desses arquivos globais (ex.:
# FallbackLocation.config da Visual Studio, em "Program Files (x86)\NuGet\Config")
# fica com permissão bloqueada para o usuário atual, e isso derruba a restauração
# mesmo sem ter nada a ver com este projeto.
release:
	@echo "Publicando Auditor Fiscal versão $(VERSION)..."
	dotnet publish $(APP_PROJECT) \
		-c Release \
		-r $(RUNTIME) \
		-p:Version=$(VERSION) \
		-p:RestoreConfigFile=NuGet.Config \
		-o "$(PUBLISH_DIR)"
	@echo "Empacotando (sem .pdb/.db — o executável single-file já contém tudo que é necessário; o banco do auditor mora em %LOCALAPPDATA%, fora do projeto, e nunca deveria estar aqui, mas a exclusão fica como cinto de segurança)..."
	powershell -NoProfile -ExecutionPolicy Bypass -Command \
		"Compress-Archive -Path (Get-ChildItem '$(PUBLISH_DIR)' -Exclude '*.pdb','*.db','*.db-wal','*.db-shm').FullName -DestinationPath '$(ZIP_FILE)' -Force"
	@echo "Pronto: $(ZIP_FILE)"

clean:
	@:; rm -rf $(DIST_DIR)

# Combina "tag" + "release" num comando só (aceita "make deploy 1.2.3.4" para uma
# versão exata, ou "make deploy" sozinho para bump automático — ver "tag" acima).
# Precisa disparar "release" numa sub-make ($(MAKE) release, não só uma dependência
# "deploy: tag release"): VERSION é calculada uma única vez, no início desta invocação
# do make — se "release" reaproveitasse essa mesma invocação, ainda pegaria a tag
# ANTIGA, mesmo depois de "tag" já ter criado a nova. A sub-make relê o Makefile do
# zero e recalcula VERSION já com a tag nova.
deploy: tag
	@$(MAKE) release
