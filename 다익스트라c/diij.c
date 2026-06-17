#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <limits.h>
#include <stdio.h>

#pragma warning(disable:4996)

#define TRUE 1
#define FALSE 0
#define MAX_VERTICES 500
#define INF 1000000
#define QUEUE_SIZE 10000
#define MAX_STATION_NAME 30
#define SUBWAY_DATA_FILE "subway_data.csv"

typedef struct {
	int id;
	char name[MAX_STATION_NAME];
	int line;
	int order; // CSV A열의 숫자
} Station;

typedef struct {
	int line;
	char name[MAX_STATION_NAME];
	double distance;
} StationDistance;

typedef struct {
	int n;
	int weight[MAX_VERTICES][MAX_VERTICES];
} GraphType;

typedef struct {
	int dist;
	int vertex;
} Element;

typedef struct {
	Element heap[QUEUE_SIZE];
	int size;
} PriorityQueue;

int distance[MAX_VERTICES];
int found[MAX_VERTICES];
int parent[MAX_VERTICES];
Station stations[MAX_VERTICES];
int station_count = 0;
StationDistance station_distances[MAX_VERTICES];
int distance_count = 0;

// 여러 위치에서 파일을 찾는 함수
FILE* find_and_open_file(const char* filename, const char* mode) {
	FILE* file = NULL;

	// 1. 현재 디렉토리에서 찾기
	file = fopen(filename, mode);
	if (file != NULL) {
		printf("파일 찾음: %s\n", filename);
		return file;
	}

	// 2. 부모 디렉토리에서 찾기
	char path_parent[256];
	snprintf(path_parent, sizeof(path_parent), "../%s", filename);
	file = fopen(path_parent, mode);
	if (file != NULL) {
		printf("파일 찾음: %s\n", path_parent);
		return file;
	}

	// 3. 프로젝트 루트 디렉토리에서 찾기
	char path_root[256];
	snprintf(path_root, sizeof(path_root), "../../%s", filename);
	file = fopen(path_root, mode);
	if (file != NULL) {
		printf("파일 찾음: %s\n", path_root);
		return file;
	}

	// 4. 한 단계 더 위의 디렉토리에서 찾기
	char path_root2[256];
	snprintf(path_root2, sizeof(path_root2), "../../../%s", filename);
	file = fopen(path_root2, mode);
	if (file != NULL) {
		printf("파일 찾음: %s\n", path_root2);
		return file;
	}

	// 파일을 찾지 못한 경우
	printf("오류: '%s' 파일을 찾을 수 없습니다. 현재 작업 디렉토리: ", filename);
	printf("프로그램 같은 폴더에 station_distance.csv 파일이 있는지 확인하세요.\n");
	return NULL;
}

// CSV에서 필드를 추출하는 헬퍼 함수 (한글 지원)
void extract_csv_field(const char* line, int field_num, char* output, int max_len) {
	int current_field = 0;
	int output_pos = 0;
	int i = 0;

	while (line[i] != '\0' && output_pos < max_len - 1) {
		if (line[i] == ',' || line[i] == '\n' || line[i] == '\r') {
			if (current_field == field_num) {
				// 뒤에서부터 공백 제거
				while (output_pos > 0 && (output[output_pos - 1] == ' ' || output[output_pos - 1] == '\t')) {
					output_pos--;
				}
				output[output_pos] = '\0';
				return;
			}
			if (line[i] == ',') {
				current_field++;
			}
			output_pos = 0;
			i++;
			continue;
		}

		if (current_field == field_num) {
			output[output_pos++] = line[i];
		}
		i++;
	}

	if (current_field == field_num) {
		// 뒤에서부터 공백 제거
		while (output_pos > 0 && (output[output_pos - 1] == ' ' || output[output_pos - 1] == '\t')) {
			output_pos--;
		}
		output[output_pos] = '\0';
	}
}

// subway_data.csv에서 모든 역과 거리 정보를 한번에 로드
int load_subway_data(const char* filename) {
	FILE* file = find_and_open_file(filename, "r");
	if (file == NULL) {
		return FALSE;
	}

	char line[500];
	int first_line = TRUE;

	while (fgets(line, sizeof(line), file)) {
		if (first_line) {
			first_line = FALSE;
			continue;
		}

		// 줄 끝의 개행 문자 제거
		size_t line_len = strlen(line);
		if (line_len > 0 && line[line_len - 1] == '\n') {
			line[line_len - 1] = '\0';
		}
		if (line_len > 1 && line[line_len - 2] == '\r') {
			line[line_len - 2] = '\0';
		}

		// CSV 형식: 전철역코드,전철역명,호선,역간거리
		// 쉼표로 필드 분리
		char code_str[20] = {0};
		char name[MAX_STATION_NAME] = {0};
		char line_str[20] = {0};
		char dist_str[20] = {0};

		extract_csv_field(line, 0, code_str, sizeof(code_str));
		extract_csv_field(line, 1, name, sizeof(name));
		extract_csv_field(line, 2, line_str, sizeof(line_str));
		extract_csv_field(line, 3, dist_str, sizeof(dist_str));

		if (strlen(code_str) == 0 || strlen(name) == 0) {
			continue;  // 유효하지 않은 행 건너뛰기
		}

		int code = atoi(code_str);
		double dist = atof(dist_str);

		if (station_count >= MAX_VERTICES) {
			printf("오류: 최대 역 개수 초과. 읽기 중단.\n");
			break;
		}

		// 호선 번호 추출 (01호선 -> 1)
		int line_num;
		if (sscanf(line_str, "%d", &line_num) == 1) {
			// 역명 정규화: 괄호 안의 내용 제거
			char normalized_name[MAX_STATION_NAME];
			strcpy(normalized_name, name);

			// 앞쪽 공백 제거
			int start = 0;
			while (start < (int)strlen(normalized_name) && normalized_name[start] == ' ') {
				start++;
			}
			if (start > 0) {
				strcpy(normalized_name, normalized_name + start);
			}

			// 괄호 처리
			char* paren = strchr(normalized_name, '(');
			if (paren != NULL) {
				*paren = '\0';
			}

			// 뒤쪽 공백 제거
			int len = strlen(normalized_name) - 1;
			while (len >= 0 && normalized_name[len] == ' ') {
				normalized_name[len] = '\0';
				len--;
			}

			// 역 정보 저장
			strcpy(stations[station_count].name, normalized_name);
			stations[station_count].line = line_num;
			stations[station_count].order = code;
			stations[station_count].id = station_count;

			// 거리 정보 저장
			station_distances[distance_count].line = line_num;
			strcpy(station_distances[distance_count].name, normalized_name);
			station_distances[distance_count].distance = dist;

			station_count++;
			distance_count++;
		}
	}

	fclose(file);

	printf("파일에서 %d개의 역과 거리 정보를 읽었습니다.\n\n", station_count);
	return station_count > 0;
}

// 각 호선 내에서 누적 거리를 계산하는 함수
double get_cumulative_distance(int line, const char* name) {
	double total_dist = 0;
	int found = 0;

	for (int i = 0; i < distance_count; i++) {
		if (station_distances[i].line == line) {
			total_dist += station_distances[i].distance;
			if (strcmp(station_distances[i].name, name) == 0) {
				found = 1;
				break;
			}
		}
	}

	return found ? total_dist : 999999; // 찾지 못하면 큰 값 반환
}

// stations 배열을 호선과 누적거리로 정렬하는 함수
void sort_stations_by_line_and_order(void) {
	// Bubble sort: 호선 먼저, 같은 호선 내에서는 누적거리로 정렬
	for (int i = 0; i < station_count - 1; i++) {
		for (int j = i + 1; j < station_count; j++) {
			int should_swap = 0;

			// 호선이 다르면 호선으로 비교
			if (stations[i].line != stations[j].line) {
				if (stations[i].line > stations[j].line) {
					should_swap = 1;
				}
			}
			// 같은 호선이면 누적거리로 비교
			else {
				double dist_i = get_cumulative_distance(stations[i].line, stations[i].name);
				double dist_j = get_cumulative_distance(stations[j].line, stations[j].name);
				if (dist_i > dist_j) {
					should_swap = 1;
				}
			}

			if (should_swap) {
				Station temp = stations[i];
				stations[i] = stations[j];
				stations[j] = temp;
			}
		}
	}

	// 정렬 후 ID 재할당
	for (int i = 0; i < station_count; i++) {
		stations[i].id = i;
	}
}

double get_station_distance(int line, const char* name) {
	for (int i = 0; i < distance_count; i++) {
		if (station_distances[i].line == line && 
			strcmp(station_distances[i].name, name) == 0) {
			return station_distances[i].distance;
		}
	}
	return 0;
}

int find_station_by_name(const char* name) {
	int found_stations[MAX_VERTICES];
	int found_count = 0;

	// 이름이 일치하는 모든 역 찾기
	for (int i = 0; i < station_count; i++) {
		if (strcmp(stations[i].name, name) == 0) {
			found_stations[found_count] = i;
			found_count++;
		}
	}

	if (found_count == 0) {
		return -1;
	}

	// 같은 이름의 역이 하나만 있으면 그것을 반환
	if (found_count == 1) {
		return found_stations[0];
	}

	// 같은 이름의 역이 여러 개 있으면 사용자가 선택하게 함
	printf("\n'%s' 역이 여러 호선에 있습니다:\n", name);
	for (int i = 0; i < found_count; i++) {
		printf("%d. %s (%d호선)\n", i + 1, stations[found_stations[i]].name, stations[found_stations[i]].line);
	}

	printf("원하는 호선을 선택하세요 (1-%d): ", found_count);
	fflush(stdout); // 출력 버퍼 비우기

	int choice = -1;
	int result = scanf("%d", &choice);

	// 입력 버퍼 정리
	if (result != 1) {
		// 잘못된 입력이 있으면 버퍼 비우기
		int c;
		while ((c = getchar()) != '\n' && c != EOF);
		choice = -1;
	}

	if (choice > 0 && choice <= found_count) {
		return found_stations[choice - 1];
	}

	// 유효하지 않은 선택이면 첫 번째 것을 반환
	printf("유효하지 않은 선택. 첫 번째 역을 사용합니다.\n");
	return found_stations[0];
}

int find_station_in_line(int line, int start_idx) {
	for (int i = start_idx; i < station_count; i++) {
		if (stations[i].line == line) {
			return i;
		}
	}
	return -1;
}

void print_all_stations() {
	printf("========================================\n");
	printf("         이용 가능한 모든 역\n");
	printf("========================================\n\n");

	for (int line = 1; line <= 9; line++) {
		printf("<%d호선>\n", line);
		int first = TRUE;
		int idx = 0;
		while ((idx = find_station_in_line(line, idx)) != -1) {
			if (!first) printf(", ");
			printf("%s", stations[idx].name);
			first = FALSE;
			idx++;
		}
		printf("\n\n");
	}
	printf("========================================\n\n");
}

void insert_min_heap(PriorityQueue* pq, Element item) {
	int i;
	pq->size++;
	i = pq->size - 1;
	while ((i != 0) && (item.dist < pq->heap[(i - 1) / 2].dist)) {
		pq->heap[i] = pq->heap[(i - 1) / 2];
		i = (i - 1) / 2;
	}
	pq->heap[i] = item;
}

Element delete_min_heap(PriorityQueue* pq) {
	int parent, child;
	Element item, temp;
	item = pq->heap[0];
	temp = pq->heap[pq->size - 1];
	pq->size--;
	parent = 0;
	while ((parent * 2 + 1) < pq->size) {
		child = parent * 2 + 1;
		if ((child + 1) < pq->size && pq->heap[child + 1].dist < pq->heap[child].dist) {
			child++;
		}
		if (temp.dist <= pq->heap[child].dist) break;
		pq->heap[parent] = pq->heap[child];
		parent = child;
	}
	pq->heap[parent] = temp;
	return item;
}

int is_empty_heap(PriorityQueue* pq) {
	return pq->size == 0;
}

int choose(PriorityQueue* pq, int found[]) {
	Element item;
	while (!is_empty_heap(pq)) {
		item = delete_min_heap(pq);
		if (!found[item.vertex]) {
			return item.vertex;
		}
	}
	return -1;
}

void dijkstra(GraphType* g, int start, int end) {
	int i, u, w;
	PriorityQueue pq;
	Element item;

	pq.size = 0;

	for (i = 0; i < g->n; i++) {
		distance[i] = INF;
		found[i] = FALSE;
		parent[i] = -1;
	}

	distance[start] = 0;

	item.dist = 0;
	item.vertex = start;
	insert_min_heap(&pq, item);

	while (!is_empty_heap(&pq)) {
		u = choose(&pq, found);
		if (u == -1) break;

		found[u] = TRUE;

		for (w = 0; w < g->n; w++) {
			if (!found[w]) {
				if (distance[u] + g->weight[u][w] < distance[w]) {
					distance[w] = distance[u] + g->weight[u][w];
					parent[w] = u;
					item.dist = distance[w];
					item.vertex = w;
					insert_min_heap(&pq, item);
				}
			}
		}
	}
}

void print_path(int start, int end) {
	if (distance[end] == INF) {
		printf("경로를 찾을 수 없습니다.\n");
		return;
	}

	printf("\n==========================================\n");
	printf("           최단경로 검색 결과\n");
	printf("==========================================\n");
	printf("출발점: %s (%d호선)\n", stations[start].name, stations[start].line);
	printf("도착점: %s (%d호선)\n", stations[end].name, stations[end].line);
	printf("최단거리: %.1f km\n", distance[end] / 10.0);

	printf("\n경로: ");
	int path[MAX_VERTICES];
	int path_count = 0;
	int current = end;
	while (current != -1) {
		path[path_count++] = current;
		current = parent[current];
	}

	// 연속된 같은 역을 필터링 (같은 이름이면서 같은 호선)
	int filtered_path[MAX_VERTICES];
	int filtered_count = 0;
	for (int i = path_count - 1; i >= 0; i--) {
		if (filtered_count == 0 || 
			strcmp(stations[path[i]].name, stations[filtered_path[filtered_count - 1]].name) != 0 ||
			stations[path[i]].line != stations[filtered_path[filtered_count - 1]].line) {
			filtered_path[filtered_count++] = path[i];
		}
	}

	for (int i = 0; i < filtered_count; i++) {
		printf("%s(%d호선)", stations[filtered_path[i]].name, stations[filtered_path[i]].line);
		if (i < filtered_count - 1) printf(" → ");
	}
	printf("\n==========================================\n\n");
}

void build_graph_from_stations(GraphType* g) {
	for (int i = 0; i < station_count; i++) {
		for (int j = 0; j < station_count; j++) {
			if (i == j) {
				g->weight[i][j] = 0;
			}
			else {
				g->weight[i][j] = INF;
			}
		}
	}

	// 같은 호선의 인접한 역들: 역간거리 정보 사용
	for (int line = 1; line <= 9; line++) {
		int prev_idx = -1;
		for (int i = 0; i < station_count; i++) {
			if (stations[i].line == line) {
				if (prev_idx != -1) {
					// 현재 역의 역간거리를 가중치로 사용
					double dist = get_station_distance(line, stations[i].name);
					if (dist > 0) {
						g->weight[prev_idx][i] = (int)(dist * 10 + 0.5); // km를 10배로 확대 (소수점 처리)
						g->weight[i][prev_idx] = (int)(dist * 10 + 0.5);
					} else {
						g->weight[prev_idx][i] = 20; // 거리 정보가 없으면 기본값 2km (10배)
						g->weight[i][prev_idx] = 20;
					}
				}
				prev_idx = i;
			}
		}
	}

	// 환승역: 같은 이름의 다른 호선의 역들
	for (int i = 0; i < station_count; i++) {
		for (int j = i + 1; j < station_count; j++) {
			if (strcmp(stations[i].name, stations[j].name) == 0 && 
				stations[i].line != stations[j].line) {
				g->weight[i][j] = 0;
				g->weight[j][i] = 0;
			}
		}
	}
}

int main(void) {
	GraphType* g = (GraphType*)malloc(sizeof(GraphType));
	if (g == NULL) {
		printf("메모리 할당 실패\n");
		return 1;
	}

	g->n = MAX_VERTICES;

	printf("==========================================\n");
	printf("   서울 지하철 최단경로 검색 시스템\n");
	printf("==========================================\n\n");

	// subway_data.csv에서 모든 역과 거리 정보를 로드
	if (!load_subway_data(SUBWAY_DATA_FILE)) {
		free(g);
		return 1;
	}

	// 역들을 호선과 순번으로 정렬
	sort_stations_by_line_and_order();

	g->n = station_count;
	build_graph_from_stations(g);

	print_all_stations();

	char start_name[MAX_STATION_NAME];
	char end_name[MAX_STATION_NAME];

	printf("출발점의 이름을 입력하세요: ");
	fgets(start_name, MAX_STATION_NAME, stdin);
	start_name[strcspn(start_name, "\n")] = 0;

	printf("도착점의 이름을 입력하세요: ");
	fgets(end_name, MAX_STATION_NAME, stdin);
	end_name[strcspn(end_name, "\n")] = 0;

	int start_id = find_station_by_name(start_name);
	int end_id = find_station_by_name(end_name);

	if (start_id == -1) {
		printf("\n오류: '%s' 역을 찾을 수 없습니다.\n", start_name);
		free(g);
		return 1;
	}

	if (end_id == -1) {
		printf("\n오류: '%s' 역을 찾을 수 없습니다.\n", end_name);
		free(g);
		return 1;
	}

	if (start_id == end_id) {
		printf("\n출발점과 도착점이 동일합니다.\n");
		free(g);
		return 1;
	}

	dijkstra(g, start_id, end_id);

	print_path(start_id, end_id);

	free(g);
	return 0;
}
