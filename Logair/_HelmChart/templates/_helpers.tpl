{{/*
Expand the name of the chart.
*/}}
{{- define "apiTemplate.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "apiTemplate.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "apiTemplate.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common Metadata
*/}}
{{- define "apiTemplate.metadata" -}}
namespace: {{ default "default" .Values.namespace }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "apiTemplate.labels" -}}
helm.sh/chart: {{ include "apiTemplate.chart" . }}
{{ include "apiTemplate.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "apiTemplate.selectorLabels" -}}
app.kubernetes.io/name: {{ include "apiTemplate.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Annotation labels
*/}}
{{- define "apiTemplate.annotations" -}}
timestamp: {{ required "Install requires a --set timestamp" .Values.timestamp | trim }}
{{- end }}


{{/*
Create a default service info.
Da Moustache leider keine ?. Operatoren kann
*/}}
{{- define "apiTemplate.service.port" -}}
{{- if .Values.service -}}
{{- default 8080 .Values.service.port -}}
{{- else -}}
{{- 8080 -}}
{{- end -}}
{{- end -}}

{{/*
Create a default service info.
Da Moustache leider keine ?. Operatoren kann
*/}}
{{- define "apiTemplate.service.listenerPort" -}}
{{- if .Values.service -}}
{{- default 4505 .Values.service.listenerPort -}}
{{- else -}}
{{- 4505 -}}
{{- end -}}
{{- end -}}

{{/*
Create a default service info.
Da Moustache leider keine ?. Operatoren kann
*/}}
{{- define "apiTemplate.service.type" -}}
{{- if .Values.service -}}
{{- default "ClusterIP" .Values.service.type -}}
{{- else -}}
{{- "ClusterIP" -}}
{{- end -}}
{{- end -}}
