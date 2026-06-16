#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <limits.h>

#pragma warning(disable:4996)

#define TRUE 1
#define FALSE 0
#define MAX_VERTICES 500
#define INF 1000000
#define QUEUE_SIZE 10000
#define MAX_STATION_NAME 30
#define CSV_FILE "stations.csv"

typedef struct {
	int id;
	char name[MAX_STATION_NAME];
	int line;
	int order; // CSV A열의 숫자
} Station;

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

int load_stations_from_file(const char* filename) {
	FILE* file = fopen(filename, "r");
	if (file == NULL) {
		printf("오류: '%s' 파일을 열 수 없습니다.\n", filename);
		return FALSE;
	}

	char line[500];
	char name[MAX_STATION_NAME];
	char english_name[100];
	char line_str[30];
	char order_str[20];
	int line_num, order;
	int first_line = TRUE;

	while (fgets(line, sizeof(line), file)) {
		if (first_line) {
			first_line = FALSE;
			continue;
		}

		// 쉼표로 구분된 CSV 형식 파싱 (쌍따옴표 제거)
		// "1707","명학","Myeonghak","01호선",...
		if (sscanf(line, "\"%19[^\"]\",\"%29[^\"]\",\"%99[^\"]\",\"%29[^\"]\"", 
			order_str, name, english_name, line_str) == 4) {

			if (station_count >= MAX_VERTICES) {
				printf("오류: 최대 역 개수 초과. 읽기 중단.\n");
				break;
			}

			// A열의 숫자와 호선 숫자 추출
			order = atoi(order_str);  // A열의 숫자
			if (sscanf(line_str, "%d", &line_num) == 1) {
				strcpy(stations[station_count].name, name);
				stations[station_count].line = line_num;
				stations[station_count].order = order;
				stations[station_count].id = station_count;
				station_count++;
			}
		}
	}

	fclose(file);

	// order 번호로 오름차순 정렬 (bubble sort)
	for (int i = 0; i < station_count - 1; i++) {
		for (int j = i + 1; j < station_count; j++) {
			if (stations[i].order > stations[j].order) {
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

	printf("파일에서 %d개의 역 정보를 읽었습니다.\n\n", station_count);
	return station_count > 0;
}

int find_station_by_name(const char* name) {
	for (int i = 0; i < station_count; i++) {
		if (strcmp(stations[i].name, name) == 0) {
			return i;
		}
	}
	return -1;
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
	printf("최단거리: %d km\n", distance[end]);

	printf("\n경로: ");
	int path[MAX_VERTICES];
	int path_count = 0;
	int current = end;
	while (current != -1) {
		path[path_count++] = current;
		current = parent[current];
	}

	// 연속된 같은 이름의 역을 필터링
	int filtered_path[MAX_VERTICES];
	int filtered_count = 0;
	for (int i = path_count - 1; i >= 0; i--) {
		if (filtered_count == 0 || strcmp(stations[path[i]].name, stations[filtered_path[filtered_count - 1]].name) != 0) {
			filtered_path[filtered_count++] = path[i];
		}
	}

	for (int i = 0; i < filtered_count; i++) {
		printf("%s", stations[filtered_path[i]].name);
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

	for (int line = 1; line <= 9; line++) {
		int prev_idx = -1;
		for (int i = 0; i < station_count; i++) {
			if (stations[i].line == line) {
				if (prev_idx != -1) {
					g->weight[prev_idx][i] = 2;
					g->weight[i][prev_idx] = 2;
				}
				prev_idx = i;
			}
		}
	}

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

	if (!load_stations_from_file(CSV_FILE)) {
		free(g);
		return 1;
	}

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
