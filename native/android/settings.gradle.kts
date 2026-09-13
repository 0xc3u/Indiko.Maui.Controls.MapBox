pluginManagement {
    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()
    }
}

dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        google()
        mavenCentral()
        maven {
            url = uri("https://api.mapbox.com/downloads/v2/releases/maven")
            // Public since the v11 era. If Mapbox ever re-enables auth, provide a secret
            // download token (sk.…, scope DOWNLOADS:READ) via MAPBOX_DOWNLOADS_TOKEN in
            // ~/.gradle/gradle.properties or as an environment variable.
            val token = providers.gradleProperty("MAPBOX_DOWNLOADS_TOKEN").orNull
                ?: System.getenv("MAPBOX_DOWNLOADS_TOKEN")
            if (!token.isNullOrBlank())
            {
                credentials {
                    username = "mapbox"
                    password = token
                }
                authentication {
                    create<BasicAuthentication>("basic")
                }
            }
        }
    }
}

rootProject.name = "IndikoMapboxKit"
include(":indikomapboxkit")
